namespace Ev.ServiceBus.Abstractions
{
    public interface IClient
    {
        /// <summary>
        ///     Gets the name of the queue/topic You'll be sending the message to.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Indicates the type of the underlying client
        /// </summary>
        ClientType ClientType { get; }
    }
}
