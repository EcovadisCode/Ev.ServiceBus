using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public class QueueOptions : ReceiverOptions
    {
        public QueueOptions(IServiceCollection serviceCollection, string queueName, bool strictMode)
            : base(serviceCollection, queueName, ClientType.Queue, strictMode)
        {
            QueueName = queueName;
        }

        /// <summary>
        /// The name of the queue.
        /// </summary>
        public string QueueName { get; }
    }
}
