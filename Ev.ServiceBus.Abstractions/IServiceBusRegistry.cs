using Ev.ServiceBus.Abstractions.Exceptions;

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
    }
}
