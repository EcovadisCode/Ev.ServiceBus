using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        public async Task ExecuteDispatches(CancellationToken token)
        {
            if (_dispatchesToSend.Any())
            {
                await _sender.SendDispatches(_dispatchesToSend, token).ConfigureAwait(false);

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

            _dispatchesToSend.Add(new Abstractions.Dispatch(messageDto)
            {
                DiagnosticId = Activity.Current?.Id
            });
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
                SessionId = sessionId,
                DiagnosticId = Activity.Current?.Id
            });
        }

        /// <inheritdoc />
        public void Publish<TMessagePayload>(
            TMessagePayload messageDto,
            Action<IDispatchContext> messageContextConfiguration)
        {
            if (messageDto == null)
            {
                throw new ArgumentNullException(nameof(messageDto));
            }

            if (messageContextConfiguration == null)
            {
                throw new ArgumentNullException(nameof(messageContextConfiguration));
            }

            var context = new DispatchContext();

            messageContextConfiguration.Invoke(context);

            _dispatchesToSend.Add(new Abstractions.Dispatch(messageDto, context)
            {
                SessionId = context.SessionId,
                CorrelationId = context.CorrelationId,
                MessageId = context.MessageId,
                DiagnosticId = context.DiagnosticId ?? Activity.Current?.Id
            });
        }
    }
}
