using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Abstractions
{
    public sealed class ServiceBusSettings
    {
        /// <summary>
        /// When false, The application will not receive or send messages.
        /// Calls that send messages will not throw and return without doing anything.
        /// (This is generally used in local debug mode, when you don't want to setup queues and topics)
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// When false, the application will not receive messages.
        /// It can still send messages.
        /// </summary>
        public bool ReceiveMessages { get; set; } = true;

        /// <summary>
        /// The current default connection settings.
        /// Use <see cref="WithConnection(string,Microsoft.Azure.ServiceBus.ReceiveMode,Microsoft.Azure.ServiceBus.RetryPolicy?)"/> to set it.
        /// </summary>
        public ConnectionSettings? ConnectionSettings { get; private set; }

        /// <summary>
        /// Sets the default Connection to use for every resource. (this can be overriden on each and every resource you want)
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="receiveMode"></param>
        /// <param name="retryPolicy"></param>
        public void WithConnection(string connectionString, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy? retryPolicy = null)
        {
            ConnectionSettings = new ConnectionSettings(connectionString, receiveMode, retryPolicy);
        }

        /// <summary>
        /// Sets the default Connection to use for every resource. (this can be overriden on each and every resource you want)
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="receiveMode"></param>
        /// <param name="retryPolicy"></param>
        public void WithConnection(ServiceBusConnection connection, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy? retryPolicy = null)
        {
            ConnectionSettings = new ConnectionSettings(connection, receiveMode, retryPolicy);
        }

        /// <summary>
        /// Sets the default Connection to use for every resource. (this can be overriden on each and every resource you want)
        /// </summary>
        /// <param name="connectionStringBuilder"></param>
        /// <param name="receiveMode"></param>
        /// <param name="retryPolicy"></param>
        public void WithConnection(ServiceBusConnectionStringBuilder connectionStringBuilder, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy? retryPolicy = null)
        {
            ConnectionSettings = new ConnectionSettings(connectionStringBuilder, receiveMode, retryPolicy);
        }
    }
}