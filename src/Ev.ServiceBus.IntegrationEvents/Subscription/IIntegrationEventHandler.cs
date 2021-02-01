using System.Threading;
using System.Threading.Tasks;

namespace Ev.ServiceBus.IntegrationEvents.Subscription
{
    public interface IIntegrationEventHandler<in TIntegrationEvent>
    {
        Task Handle(TIntegrationEvent @event, CancellationToken cancellationToken);
    }
}
