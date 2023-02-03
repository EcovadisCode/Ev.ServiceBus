using System;

namespace Ev.ServiceBus.Abstractions
{
    public interface IServiceBusRegistry
    {
        /// <summary>
        ///     Retrieves a registered queue sender.
        /// </summary>
        /// <param name="name">The name of the registered queue.</param>
        /// <exception cref="QueueSenderNotFoundException"></exception>
        /// <returns>An object allowing you to send messages to this queue.</returns>
        IMessageSender GetQueueSender(string name);

        /// <summary>
        ///     Retrieves a registered topic sender.
        /// </summary>
        /// <param name="name">The name of the registered topic.</param>
        /// <exception cref="TopicSenderNotFoundException"></exception>
        /// <returns>An object allowing you to send messages to this topic.</returns>
        IMessageSender GetTopicSender(string name);

        /// <summary>
        /// Retrieves a registered topic sender.
        /// </summary>
        /// <param name="clientType"></param>
        /// <param name="resourceId">The name of the registered sender.</param>
        /// <returns>An object allowing you to send messages to this resource.</returns>
        IMessageSender GetSender(ClientType clientType, string resourceId);

        /// <summary>
        /// Gets the registrations for a specific dispatch contract
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        MessageDispatchRegistration[] GetDispatchRegistrations(Type messageType);

        /// <summary>
        /// Gets the registration for a specific reception contract.
        /// </summary>
        /// <param name="payloadTypeId"></param>
        /// <param name="receiverName"></param>
        /// <param name="clientType"></param>
        /// <returns></returns>
        MessageReceptionRegistration? GetReceptionRegistration(string payloadTypeId, string receiverName, ClientType clientType);
    }
}
