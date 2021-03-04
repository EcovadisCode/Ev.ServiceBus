using System;
using System.Collections.Generic;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.HealthChecks
{
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

            return x.ConnectionString == y.ConnectionString;
        }

        public int GetHashCode(ConnectionSettings? obj)
        {
            return obj?.ConnectionString?.GetHashCode() ?? 0;
        }
    }
}
