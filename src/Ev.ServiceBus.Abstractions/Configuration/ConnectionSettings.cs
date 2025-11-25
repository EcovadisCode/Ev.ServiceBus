using System;
using Azure.Messaging.ServiceBus;
using Azure.Core;

namespace Ev.ServiceBus.Abstractions.Configuration;

public class ConnectionSettings
{
    internal ConnectionSettings(string connectionString, ServiceBusClientOptions options)
    {
        ConnectionString = connectionString;
        Options = options;
        Endpoint = ServiceBusConnectionStringProperties.Parse(connectionString).Endpoint.AbsoluteUri;
    }

    internal ConnectionSettings(string fullyQualifiedNamespace, TokenCredential credentials, ServiceBusClientOptions options)
    {
        if (!fullyQualifiedNamespace.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
        {
            fullyQualifiedNamespace = $"Endpoint={fullyQualifiedNamespace}";
        }

        var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(fullyQualifiedNamespace);

        Options = options;
        FullyQualifiedNamespace = connectionStringProperties.FullyQualifiedNamespace;
        Credentials = credentials;
        Endpoint = connectionStringProperties.Endpoint.AbsoluteUri;
    }

    public string Endpoint { get; }

    public string? ConnectionString { get; }

    public ServiceBusClientOptions? Options { get; }

    public string? FullyQualifiedNamespace { get; }

    public TokenCredential? Credentials { get; }

    private bool Equals(ConnectionSettings other) =>
        string.Equals(Endpoint, other.Endpoint, StringComparison.Ordinal)
        && string.Equals(ConnectionString, other.ConnectionString, StringComparison.Ordinal)
        && Options != null
        && Options.Equals(other.Options)
        && string.Equals(FullyQualifiedNamespace, other.FullyQualifiedNamespace, StringComparison.Ordinal)
        && Equals(Credentials, other.Credentials);

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ConnectionSettings)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Endpoint,
            ConnectionString,
            Options,
            FullyQualifiedNamespace,
            Credentials);
    }
}