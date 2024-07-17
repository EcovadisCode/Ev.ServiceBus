using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Dispatch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ev.ServiceBus;

public class ServiceBusBuilder
{
    public IServiceCollection Services { get; }

    public ServiceBusBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// Registers a listener that will be called each time the execution of a message starts, has succeed or fails.
    /// </summary>
    /// <typeparam name="TEventListener"></typeparam>
    /// <returns></returns>
    public ServiceBusBuilder RegisterEventListener<TEventListener>() where TEventListener : class, IServiceBusEventListener
    {
        Services.AddScoped<IServiceBusEventListener, TEventListener>();
        return this;
    }

    /// <summary>
    /// Registers a listener that will be called on every dispatch sending giving you the ability to update the outgoing message before sending.
    /// </summary>
    /// <typeparam name="TExtenderService"></typeparam>
    /// <returns></returns>
    public ServiceBusBuilder RegisterDispatchExtender<TExtenderService>()
        where TExtenderService : class, IDispatchExtender
    {
        Services.AddScoped<IDispatchExtender, TExtenderService>();
        return this;
    }

    /// <summary>
    /// Defines a serializer to be used for every payload serialization and deserialization.
    /// By default System.Text.Json is used
    /// </summary>
    /// <typeparam name="TMessagePayloadSerializer"></typeparam>
    /// <returns></returns>
    public ServiceBusBuilder WithPayloadSerializer<TMessagePayloadSerializer>()
        where TMessagePayloadSerializer : class, IMessagePayloadSerializer
    {
        Services.Replace(new ServiceDescriptor(typeof(IMessagePayloadSerializer), typeof(TMessagePayloadSerializer), ServiceLifetime.Singleton));
        return this;
    }
}