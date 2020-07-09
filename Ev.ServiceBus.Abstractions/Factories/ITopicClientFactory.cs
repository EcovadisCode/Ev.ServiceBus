using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public interface ITopicClientFactory
    {
        ITopicClient Create(TopicOptions options);
    }
}
