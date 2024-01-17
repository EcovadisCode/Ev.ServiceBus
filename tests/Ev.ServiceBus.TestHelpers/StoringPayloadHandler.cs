using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.UnitTests.Helpers;

namespace Ev.ServiceBus.TestHelpers;

public class StoringPayloadHandler<TEvent> : IMessageReceptionHandler<TEvent>
{
    private readonly EventStore _store;

    public StoringPayloadHandler(EventStore store)
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