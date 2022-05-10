using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus;

public class ClientFactory : IClientFactory
{
    public ServiceBusClient Create(ConnectionSettings connectionSettings)
    {
        if (connectionSettings.Options != null)
        {
            return new ServiceBusClient(connectionSettings.ConnectionString, connectionSettings.Options);
        }

        return new ServiceBusClient(connectionSettings.ConnectionString);
    }
}
