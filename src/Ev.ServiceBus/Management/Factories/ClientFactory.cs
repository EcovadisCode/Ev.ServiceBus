using System;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus;

public class ClientFactory : IClientFactory
{
    public ServiceBusClient Create(ConnectionSettings connectionSettings)
    {
        if (!string.IsNullOrWhiteSpace(connectionSettings.ConnectionString))
        {
            return connectionSettings.Options is not null
                ? new ServiceBusClient(connectionSettings.ConnectionString, connectionSettings.Options)
                : new ServiceBusClient(connectionSettings.ConnectionString);
        }

        if(connectionSettings.Credentials is not null && !string.IsNullOrWhiteSpace(connectionSettings.FullyQualifiedNamespace))
        {
            return connectionSettings.Options is not null
                ? new ServiceBusClient(connectionSettings.FullyQualifiedNamespace, connectionSettings.Credentials, connectionSettings.Options)
                : new ServiceBusClient(connectionSettings.FullyQualifiedNamespace, connectionSettings.Credentials);
        }

        throw new InvalidOperationException("Insufficient connection settings: provide either a connection string or both FullyQualifiedNamespace and Credentials.");
    }
}