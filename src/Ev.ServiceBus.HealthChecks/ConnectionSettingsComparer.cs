using System;
using System.Collections.Generic;
using Ev.ServiceBus.Abstractions.Configuration;

namespace Ev.ServiceBus.HealthChecks;

internal class ConnectionSettingsComparer : IEqualityComparer<ConnectionSettings?>
{
    public bool Equals(ConnectionSettings? x, ConnectionSettings? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (ReferenceEquals(x, null))
        {
            return false;
        }

        if (ReferenceEquals(y, null))
        {
            return false;
        }

        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return x.ConnectionString == y.ConnectionString
            && x.FullyQualifiedNamespace == y.FullyQualifiedNamespace
            && x.Credentials == y.Credentials;
    }

    public int GetHashCode(ConnectionSettings? obj)
    {
        return HashCode.Combine(
            obj?.ConnectionString,
            obj?.FullyQualifiedNamespace,
            obj?.Credentials);
    }
}