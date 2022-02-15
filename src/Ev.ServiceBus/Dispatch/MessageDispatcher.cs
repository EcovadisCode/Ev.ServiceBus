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
        private readonly List<Abstractions.Dispatch> _dispatchesToSend;

        public MessageDispatcher(IDispatchSender sender)
        {
            _sender = sender;
            _dispatchesToSend = new List<Abstractions.Dispatch>();
        }

        /// <inheritdoc />
        public async Task ExecuteDispatches()
        {
            if (_dispatchesToSend.Any())
            {
                await _sender.SendDispatches(_dispatchesToSend).ConfigureAwait(false);

                _dispatchesToSend.Clear();
            }
        }

        /// <inheritdoc />
        public void Publish<TMessageDto>(TMessageDto messageDto)
        {
            if (messageDto == null)
            {
                throw new ArgumentNullException(nameof(messageDto));
            }

            _dispatchesToSend.Add(new Abstractions.Dispatch(messageDto));
        }

        /// <inheritdoc />
        public void Publish<TMessagePayload>(TMessagePayload messageDto, string sessionId)
        {
            if (messageDto == null)
            {
                throw new ArgumentNullException(nameof(messageDto));
            }

            if (sessionId == null)
            {
                throw new ArgumentNullException(nameof(sessionId));
            }

            _dispatchesToSend.Add(new Abstractions.Dispatch(messageDto)
            {
                SessionId = sessionId
            });
        }
    }
}
