using System;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions;

public class ConnectionSettings
{
    internal ConnectionSettings(string connectionString, ServiceBusClientOptions options)
    {
        ConnectionString = connectionString;
        Options = options;
        Endpoint = GetEndpointFromConnectionString(connectionString);
    }

    public string Endpoint { get; }
    public string ConnectionString { get; }
    public ServiceBusClientOptions Options { get; }

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

    public override int GetHashCode()
    {
        return Endpoint.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ConnectionSettings settings)
        {
            return false;
        }
        return Endpoint.Equals(settings.Endpoint);
    }
}