using System;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.UnitTests
{
    public class FailingClientFactory : IClientFactory
    {
        public IClientEntity Create(ClientOptions options, ConnectionSettings connectionSettings)
        {
            throw new Exception();
        }
    }
}
