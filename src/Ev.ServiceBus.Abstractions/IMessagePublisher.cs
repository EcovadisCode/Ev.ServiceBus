using System;

namespace Ev.ServiceBus.Abstractions;

public interface IMessagePublisher
{
    /// <summary>
    /// Temporarily stores the object to send through Service Bus until <see cref="IMessageDispatcher.ExecuteDispatches"/> is called.
    /// </summary>
    /// <param name="messageDto">The object to send through Service Bus</param>
    /// <typeparam name="TMessagePayload">A type of object that is registered within Ev.ServiceBus</typeparam>
    void Publish<TMessagePayload>(TMessagePayload messageDto);

    /// <summary>
    /// Temporarily stores the object to send through Service Bus until <see cref="IMessageDispatcher.ExecuteDispatches"/> is called.
    /// </summary>
    /// <param name="messageDto">The object to send through Service Bus</param>
    /// <param name="sessionId">The sessionId to attach to the outgoing message</param>
    /// <typeparam name="TMessagePayload">A type of object that is registered within Ev.ServiceBus</typeparam>
    void Publish<TMessagePayload>(TMessagePayload messageDto, string sessionId);

    /// <summary>
    /// Temporarily stores the object to send through Service Bus until <see cref="IMessageDispatcher.ExecuteDispatches"/> is called.
    /// </summary>
    /// <param name="messageDto">The object to send through Service Bus</param>
    /// <param name="messageContextConfiguration">Configurator of message context</param>
    /// <typeparam name="TMessagePayload">A type of object that is registered within Ev.ServiceBus</typeparam>
    void Publish<TMessagePayload>(TMessagePayload messageDto, Action<IDispatchContext> messageContextConfiguration);
}