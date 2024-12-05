using System;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ev.ServiceBus.Reception;

public class ReceptionRegistrationBuilder
{
    private readonly ReceiverOptions _options;
    private readonly IServiceCollection _services;

    public ReceptionRegistrationBuilder(IServiceCollection services, ReceiverOptions receiverOptions)
    {
        _services = services;
        _options = receiverOptions;
    }

    /// <summary>
    /// Change the settings of the underlying message handler.
    /// </summary>
    /// <param name="config"></param>
    public void CustomizeMessageHandling(Action<ServiceBusProcessorOptions> config)
    {
        _options.WithCustomHandlerOptions(config);
    }

    /// <summary>
    /// Sets a specific connection for the underlying resource.
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="connectionOptions"></param>
    public void CustomizeConnection(string connectionString, ServiceBusClientOptions connectionOptions)
    {
        _options.WithConnection(connectionString, connectionOptions);
    }

    /// <summary>
    /// Registers a class as a payload to receive and deserialize through the current resource.
    /// </summary>
    /// <typeparam name="TReceptionModel">The class to deserialize the message into</typeparam>
    /// <typeparam name="THandler">The handler that will receive the deserialized object</typeparam>
    /// <returns></returns>
    public MessageReceptionBuilder RegisterReception<TReceptionModel, THandler>()
        where THandler : class, IMessageReceptionHandler<TReceptionModel>
    {
        _services.TryAddScoped<THandler>();
        var builder = new MessageReceptionBuilder(_options, typeof(TReceptionModel), typeof(THandler));
        _services.Configure<ServiceBusOptions>(options =>
        {
            options.RegisterReception(builder);
        });
        return builder;
    }

    /// <summary>
    /// Registers a generic handler to receive message from a given PayloadTypeId.
    /// </summary>
    /// <param name="payloadTypeId"></param>
    /// <typeparam name="THandler">The handler that will receive the raw data</typeparam>
    /// <returns></returns>
    public MessageReceptionBuilder RegisterReception<THandler>(string payloadTypeId)
        where THandler : class, IMessageReceptionHandler
    {
        _services.TryAddScoped<THandler>();
        var builder = new MessageReceptionBuilder(_options, payloadTypeId, typeof(THandler));
        _services.Configure<ServiceBusOptions>(options =>
        {
            options.RegisterReception(builder);
        });
        return builder;
    }

    /// <summary>
    /// Registers a class as a payload to receive and deserialize through the current resource.
    /// </summary>
    /// <returns></returns>
    public MessageReceptionBuilder RegisterReception(Type receptionModel, Type handlerType)
    {
        if (receptionModel == null)
        {
            throw new ArgumentNullException(nameof(receptionModel));
        }

        if (handlerType == null)
        {
            throw new ArgumentNullException(nameof(handlerType));
        }

        var handlerInterface = typeof(IMessageReceptionHandler<>).MakeGenericType(receptionModel);
        if (handlerInterface.IsAssignableFrom(handlerType) == false)
        {
            throw new ArgumentException($"{nameof(handlerType)} must implement IMessageReceptionHandler<{nameof(receptionModel)}> interface", nameof(handlerType));
        }

        _services.TryAddScoped(handlerType);
        var builder = new MessageReceptionBuilder(_options, receptionModel, handlerType);
        _services.Configure<ServiceBusOptions>(options =>
        {
            options.RegisterReception(builder);
        });
        return builder;
    }

    /// <summary>
    /// Activates session handling mode. Be careful, session must be enabled on the resource itself also.
    /// </summary>
    /// <param name="config"></param>
    public void EnableSessionHandling(Action<ServiceBusSessionProcessorOptions> config)
    {
        _options.EnableSessionHandling(config);
    }
}