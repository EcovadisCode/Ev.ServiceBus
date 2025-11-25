using System;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions.Configuration;

namespace Ev.ServiceBus.UnitTests.Helpers;

public class FailingClientFactory : IClientFactory
{
    public ServiceBusClient Create(ConnectionSettings connectionSettings)
    {
        throw new Exception();
    }
}