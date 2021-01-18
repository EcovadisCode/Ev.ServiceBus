using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Abstractions
{
    public sealed class ServiceBusSettings
    {
        public bool Enabled { get; set; } = true;
        public bool ReceiveMessages { get; set; } = true;

        public ConnectionSettings? ConnectionSettings { get; private set; }

        public void WithConnection(string connectionString, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy? retryPolicy = null)
        {
            ConnectionSettings = new ConnectionSettings(connectionString, receiveMode, retryPolicy);
        }

        public void WithConnection(ServiceBusConnection connection, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy? retryPolicy = null)
        {
            ConnectionSettings = new ConnectionSettings(connection, receiveMode, retryPolicy);
        }

        public void WithConnection(ServiceBusConnectionStringBuilder connectionStringBuilder, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy? retryPolicy = null)
        {
            ConnectionSettings = new ConnectionSettings(connectionStringBuilder, receiveMode, retryPolicy);
        }
    }
}