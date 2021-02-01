using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.IntegrationEvents.Subscription;

namespace Ev.ServiceBus.IntegrationEvents.UnitTests.Helpers
{
    public class StoringEventHandler<TEvent> : IIntegrationEventHandler<TEvent>
    {
        private readonly EventStore _store;

        public StoringEventHandler(EventStore store)
        {
            _store = store;
        }

        public Task Handle(TEvent @event, CancellationToken cancellationToken)
        {
            _store.Events.Add(
                new EventStore.Item
                {
                    Event = @event,
                    HandlerType = GetType()
                });
            return Task.CompletedTask;
        }
    }
}
