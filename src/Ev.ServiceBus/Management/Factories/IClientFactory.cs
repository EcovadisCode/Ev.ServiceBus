using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus;

public interface IClientFactory
{
    ServiceBusClient Create(ConnectionSettings connectionSettings);
}