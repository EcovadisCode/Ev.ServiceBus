using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.IntegrationEvents.Subscription;

namespace Ev.ServiceBus.IntegrationEvents.UnitTests.Helpers
{
    public class NoiseEvent
    {
    }

    public class SubscribedEvent
    {
        public string SomeString { get; set; }
        public int SomeNumber { get; set; }
    }

    public class NoiseHandler : StoringEventHandler<NoiseEvent>
    {
        public NoiseHandler(EventStore store) : base(store) { }
    }

    public class SubscribedEventHandler : IIntegrationEventHandler<SubscribedEvent>
    {
        public Task Handle(SubscribedEvent @event, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
