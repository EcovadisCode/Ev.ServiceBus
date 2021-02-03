using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public class IntegrationEventDispatcher : IIntegrationEventPublisher, IIntegrationEventDispatcher
    {
        private readonly IIntegrationEventSender _sender;
        private readonly List<object> _eventsToSend;

        public IntegrationEventDispatcher(IIntegrationEventSender sender)
        {
            _sender = sender;
            _eventsToSend = new List<object>();
        }

        public async Task DispatchEvents()
        {
            if (_eventsToSend.Any())
            {
                await _sender.SendEvents(_eventsToSend).ConfigureAwait(false);

                _eventsToSend.Clear();
            }
        }

        public void Publish<TMessageDto>(TMessageDto messageDto)
        {
            if (messageDto == null)
            {
                throw new ArgumentNullException(nameof(messageDto));
            }

            _eventsToSend.Add(messageDto);
        }
    }
}
