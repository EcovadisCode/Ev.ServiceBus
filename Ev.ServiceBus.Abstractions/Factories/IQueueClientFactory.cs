using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public interface IQueueClientFactory
    {
        IQueueClient Create(QueueOptions options);
    }
}
