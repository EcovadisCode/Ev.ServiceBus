using System;

namespace Ev.ServiceBus.Abstractions;

public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message)
    {
    }
}
