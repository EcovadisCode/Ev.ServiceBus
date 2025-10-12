using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions.Configuration;

public class ConnectionSettings
{
    internal ConnectionSettings(string connectionString, ServiceBusClientOptions options)
    {
        ConnectionString = connectionString;
        Options = options;
        Endpoint = ServiceBusConnectionStringProperties.Parse(connectionString).Endpoint.AbsoluteUri;
    }

    public string Endpoint { get; }
    public string ConnectionString { get; }
    public ServiceBusClientOptions Options { get; }

    public override int GetHashCode()
    {
        return Endpoint.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is ConnectionSettings settings && Endpoint.Equals(settings.Endpoint);
    }
}