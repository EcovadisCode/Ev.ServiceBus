using System;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public class FailingClientFactory<TOptions, TClient> : IClientFactory<TOptions, TClient>
        where TClient : IClientEntity
        where TOptions : ClientOptions
    {
        public TClient Create(TOptions options, ConnectionSettings connectionSettings)
        {
            throw new Exception();
        }
    }
}
