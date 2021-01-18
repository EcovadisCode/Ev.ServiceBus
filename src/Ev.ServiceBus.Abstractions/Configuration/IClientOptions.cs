// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public interface IClientOptions
    {
        string EntityPath { get; }
        ClientType ClientType { get; }
        public ConnectionSettings? ConnectionSettings { get; }
    }
}