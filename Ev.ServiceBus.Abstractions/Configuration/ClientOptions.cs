

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public abstract class ClientOptions : IClientOptions
    {
        protected ClientOptions(string entityPath, ClientType clientType)
        {
            EntityPath = entityPath;
            ClientType = clientType;
        }

        public string EntityPath { get; }
        public ClientType ClientType { get; }
        public ConnectionSettings? ConnectionSettings { get; internal set; }
    }
}
