using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public interface IIntegrationEventSender
    {
        Task SendEvents(IReadOnlyList<KeyValuePair<EventPublicationRegistration, object>> events);
    }
}
