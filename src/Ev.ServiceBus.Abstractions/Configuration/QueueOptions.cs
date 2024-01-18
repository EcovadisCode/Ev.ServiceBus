using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions;

public class QueueOptions : ReceiverOptions
{
    public QueueOptions(IServiceCollection serviceCollection, string queueName)
        : base(serviceCollection, queueName, ClientType.Queue)
    {
        QueueName = queueName;
    }

    /// <summary>
    /// The name of the queue.
    /// </summary>
    public string QueueName { get; }
}