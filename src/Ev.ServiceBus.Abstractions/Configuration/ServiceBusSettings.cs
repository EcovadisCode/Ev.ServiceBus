using Azure.Messaging.ServiceBus;

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
        /// <param name="options"></param>
        public void WithConnection(string connectionString, ServiceBusClientOptions options)
        {
            ConnectionSettings = new ConnectionSettings(connectionString, options);
        }
    }
}