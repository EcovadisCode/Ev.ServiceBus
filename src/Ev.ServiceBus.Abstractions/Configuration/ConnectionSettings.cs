using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Abstractions
{
    public class ConnectionSettings
    {
        internal ConnectionSettings(
            ServiceBusConnection serviceBusConnection,
            ReceiveMode receiveMode,
            RetryPolicy? retryPolicy)
        {
            Connection = serviceBusConnection;
            ReceiveMode = receiveMode;
            RetryPolicy = retryPolicy;
        }

        internal ConnectionSettings(
            ServiceBusConnectionStringBuilder serviceBusConnectionStringBuilder,
            ReceiveMode receiveMode,
            RetryPolicy? retryPolicy)
        {
            ConnectionStringBuilder = serviceBusConnectionStringBuilder;
            ReceiveMode = receiveMode;
            RetryPolicy = retryPolicy;
        }

        internal ConnectionSettings(string connectionString, ReceiveMode receiveMode, RetryPolicy? retryPolicy)
        {
            ConnectionString = connectionString;
            ReceiveMode = receiveMode;
            RetryPolicy = retryPolicy;
        }

        public string? ConnectionString { get; }
        public ReceiveMode ReceiveMode { get; }
        public RetryPolicy? RetryPolicy { get; }
        public ServiceBusConnection? Connection { get; }
        public ServiceBusConnectionStringBuilder? ConnectionStringBuilder { get; }
    }
}
