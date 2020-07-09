using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public abstract class ClientOptions : IClientOptions
    {
        protected ClientOptions(string entityPath)
        {
            EntityPath = entityPath;
            ReceiveMode = ReceiveMode.PeekLock;
            RetryPolicy = RetryPolicy.Default;
        }

        public string EntityPath { get; }
        public ReceiveMode ReceiveMode { get; internal set; }
        public RetryPolicy RetryPolicy { get; internal set; }

        public ServiceBusConnection Connection { get; internal set; }
        public ServiceBusConnectionStringBuilder ConnectionStringBuilder { get; internal set; }
        public string ConnectionString { get; internal set; }
    }
}
