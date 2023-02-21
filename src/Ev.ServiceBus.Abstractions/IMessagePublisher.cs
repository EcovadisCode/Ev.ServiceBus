using System;

namespace Ev.ServiceBus.Abstractions
{
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
        /// <param name="diagnosticId">Unique identifier of an external call from producer to the queue. Refer to W3C Trace-Context traceparent header for the format</param>
        /// <typeparam name="TMessagePayload">A type of object that is registered within Ev.ServiceBus</typeparam>
        void Publish<TMessagePayload>(TMessagePayload messageDto, string sessionId, string? diagnosticId = null);

        /// <summary>
        /// Temporarily stores the object to send through Service Bus until <see cref="IMessageDispatcher.ExecuteDispatches"/> is called.
        /// </summary>
        /// <param name="messageDto">The object to send through Service Bus</param>
        /// <param name="messageContextConfiguration">Configurator of message context</param>
        /// <typeparam name="TMessagePayload">A type of object that is registered within Ev.ServiceBus</typeparam>
        void Publish<TMessagePayload>(TMessagePayload messageDto, Action<IMessageContext> messageContextConfiguration);
    }
}
