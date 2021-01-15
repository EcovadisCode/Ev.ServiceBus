using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public interface IClientFactory
    {
        IClientEntity Create(ClientOptions options, ConnectionSettings connectionSettings);
    }
}
