using System;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Ev.ServiceBus.Dispatch;

public class DispatchRegistrationBuilder
{
    private readonly IServiceCollection _services;
    private readonly ClientOptions _options;

    public DispatchRegistrationBuilder(IServiceCollection services, ClientOptions options)
    {
        _services = services;
        _options = options;
    }

    /// <summary>
    /// Sets a specific connection for the underlying resource.
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="options"></param>
    public void CustomizeConnection(
        string connectionString,
        ServiceBusClientOptions options)
    {
        _options.WithConnection(connectionString, options);
    }

    /// <summary>
    /// Sets a specific connection using Azure Entra authorization for the underlying resource.
    /// </summary>
    /// <param name="fullyQualifiedNamespace"></param>
    /// <param name="credentials"></param>
    /// <param name="options"></param>
    public void CustomizeConnection(
        string fullyQualifiedNamespace,
        Azure.Core.TokenCredential credentials,
        ServiceBusClientOptions options)
    {
        _options.WithConnection(fullyQualifiedNamespace, credentials, options);
    }

    /// <summary>
    /// Registers a class as a payload to serialize and send through the current resource.
    /// </summary>
    /// <typeparam name="TDispatchModel">The class to serialize the message into</typeparam>
    /// <returns></returns>
    public MessageDispatchRegistration RegisterDispatch<TDispatchModel>()
    {
        var builder = new MessageDispatchRegistration(_options, typeof(TDispatchModel));
        _services.Configure<ServiceBusOptions>(options =>
        {
            options.RegisterDispatch(builder);
        });
        return builder;
    }

    /// <summary>
    /// Registers a class as a payload to serialize and send through the current resource.
    /// </summary>
    /// <returns></returns>
    public MessageDispatchRegistration RegisterDispatch(Type dispatchModel)
    {
        if (dispatchModel == null)
        {
            throw new ArgumentNullException(nameof(dispatchModel));
        }
        var builder = new MessageDispatchRegistration(_options, dispatchModel);
        _services.Configure<ServiceBusOptions>(options =>
        {
            options.RegisterDispatch(builder);
        });
        return builder;
    }
}