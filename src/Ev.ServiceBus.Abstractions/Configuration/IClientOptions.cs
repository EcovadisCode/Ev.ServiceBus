// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public interface IClientOptions
    {
        string ResourceId { get; }
        ClientType ClientType { get; }
        public ConnectionSettings? ConnectionSettings { get; }
    }
}