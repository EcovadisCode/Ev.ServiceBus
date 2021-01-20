using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus
{
    public interface IClientFactory<in TClientOptions, out TClientEntity>
        where TClientOptions : ClientOptions
        where TClientEntity : IClientEntity
    {
        TClientEntity Create(TClientOptions options, ConnectionSettings connectionSettings);
    }
}
