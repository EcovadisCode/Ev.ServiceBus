using System;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.TestHelpers;

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
            Thread.Sleep(1);
            return Task.CompletedTask;
        }
    }

    public class SubscribedEventThrowingHandler : IMessageReceptionHandler<SubscribedEvent>
    {
        public Task Handle(SubscribedEvent @event, CancellationToken cancellationToken)
        {
            Thread.Sleep(1);
            throw new ArgumentOutOfRangeException();
        }
    }
}
