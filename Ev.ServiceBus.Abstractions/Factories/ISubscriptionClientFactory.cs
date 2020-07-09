using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public interface ISubscriptionClientFactory
    {
        ISubscriptionClient Create(SubscriptionOptions options);
    }
}
