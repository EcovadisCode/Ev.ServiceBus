using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Dispatch
{
    public class MessageDispatcher : IMessagePublisher, IMessageDispatcher
    {
        private readonly IDispatchSender _sender;
        private readonly List<object> _eventsToSend;

        public MessageDispatcher(IDispatchSender sender)
        {
            _sender = sender;
            _eventsToSend = new List<object>();
        }

        /// <inheritdoc />
        public async Task ExecuteDispatches()
        {
            if (_eventsToSend.Any())
            {
                await _sender.SendDispatches(_eventsToSend).ConfigureAwait(false);

                _eventsToSend.Clear();
            }
        }

        /// <inheritdoc />
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
