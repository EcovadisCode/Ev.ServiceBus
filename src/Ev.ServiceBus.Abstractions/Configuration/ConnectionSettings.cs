using Azure.Core;
using Azure.Messaging.ServiceBus;
using System;

namespace Ev.ServiceBus.Abstractions;

public class ConnectionSettings
{
    internal ConnectionSettings(string connectionString, ServiceBusClientOptions options)
    {
        ConnectionString = connectionString;
        Options = options;
        Endpoint = GetEndpointFromConnectionString(connectionString);
    }

    internal ConnectionSettings(string fullyQualifiedNamespace, TokenCredential credentials, ServiceBusClientOptions options)
    {
        Options = options;
        FullyQualifiedNamespace = fullyQualifiedNamespace;
        Credentials = credentials;
        Endpoint = GetEndpointFromFullyQualifiedNamespace(fullyQualifiedNamespace);
    }

    public string Endpoint { get; }

    public string? ConnectionString { get; }

    public ServiceBusClientOptions? Options { get; }

    public string? FullyQualifiedNamespace { get; }

    public TokenCredential? Credentials { get; }

    private string GetEndpointFromConnectionString(string connectionString)
    {
        var KeyValuePairDelimiter = ';';
        var KeyValueSeparator = '=';
        var EndpointConfigName = "Endpoint";

        // First split based on ';'
        var keyValuePairs = connectionString.Split(new[] { KeyValuePairDelimiter }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var keyValuePair in keyValuePairs)
        {
            // Now split based on the _first_ '='
            var keyAndValue = keyValuePair.Split(new[] { KeyValueSeparator }, 2);
            var key = keyAndValue[0];
            if (keyAndValue.Length != 2)
            {
                return string.Empty;
            }

            var value = keyAndValue[1].Trim();
            if (key.Equals(EndpointConfigName, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }
        return string.Empty;
    }

    private string GetEndpointFromFullyQualifiedNamespace(string fullyQualifiedNamespace)
    {
        return $"sb://{fullyQualifiedNamespace}/";
    }

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