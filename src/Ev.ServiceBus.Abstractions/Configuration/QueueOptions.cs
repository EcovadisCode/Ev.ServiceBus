using System;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public class QueueOptions : ReceiverOptions
    {
        private readonly IServiceCollection _serviceCollection;

        public QueueOptions(IServiceCollection serviceCollection, string queueName)
            : base(serviceCollection, queueName, ClientType.Queue)
        {
            _serviceCollection = serviceCollection;
            QueueName = queueName;
        }

        public string QueueName { get; }
    }
}
