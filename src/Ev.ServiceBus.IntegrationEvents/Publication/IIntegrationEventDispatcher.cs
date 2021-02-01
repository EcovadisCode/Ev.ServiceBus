using System.Threading.Tasks;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public interface IIntegrationEventDispatcher
    {
        Task DispatchEvents();
    }
}
