using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Reception;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public class NoiseEvent
    {
    }

    public class SubscribedEvent
    {
        public string SomeString { get; set; }
        public int SomeNumber { get; set; }
    }

    public class NoiseHandler : StoringPayloadHandler<NoiseEvent>
    {
        public NoiseHandler(EventStore store) : base(store) { }
    }

    public class SubscribedEventHandler : IMessageReceptionHandler<SubscribedEvent>
    {
        public Task Handle(SubscribedEvent @event, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
