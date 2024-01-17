using System;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions;

public abstract class ReceiverOptions : ClientOptions, IMessageReceiverOptions
{
    private readonly IServiceCollection _services;

    protected ReceiverOptions(IServiceCollection services, string resourceId, ClientType clientType)
        : base(resourceId, clientType)
    {
        _services = services;
    }

    /// <inheritdoc />
    public Action<ServiceBusProcessorOptions>? ServiceBusProcessorOptions { get; private set; }

    /// <inheritdoc />
    public Type? ExceptionHandlerType { get; private set; }

    public Action<ServiceBusSessionProcessorOptions>? SessionProcessorOptions { get; private set; }

    /// <summary>
    /// Defines a message handler for the current receiver.
    /// </summary>
    /// <param name="config"></param>
    /// <typeparam name="TMessageHandler"></typeparam>
    internal void WithCustomHandlerOptions(Action<ServiceBusProcessorOptions> config)
    {
        ServiceBusProcessorOptions = config;
    }

    /// <summary>
    /// Activates session handling for the current receiver.
    /// </summary>
    public void EnableSessionHandling(Action<ServiceBusSessionProcessorOptions> config)
    {
        SessionProcessorOptions = config;
    }
}