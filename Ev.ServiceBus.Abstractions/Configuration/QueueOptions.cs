using System;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public class QueueOptions : ReceiverOptions
    {
        public QueueOptions(string queueName) : base(queueName)
        {
            QueueName = queueName;
        }

        public string QueueName { get; }

        public QueueOptions WithCustomMessageHandler<TMessageHandler>(Action<MessageHandlerOptions> config = null)
            where TMessageHandler : IMessageHandler
        {
            MessageHandlerType = typeof(TMessageHandler);
            MessageHandlerConfig = config;
            return this;
        }

        public QueueOptions WithCustomExceptionHandler<TExceptionHandler>()
            where TExceptionHandler : IExceptionHandler
        {
            ExceptionHandlerType = typeof(TExceptionHandler);
            return this;
        }
    }
}
