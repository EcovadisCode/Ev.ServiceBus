using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Dispatch.Outbox;

public class OutboxMessagePublisher : IMessagePublisher
{
    private readonly IMessagePublisher _underlyingMessagePublisher;
    private readonly IDispatchSender _dispatchSender;

    public OutboxMessagePublisher(
        IMessagePublisher underlyingMessagePublisher,
        IDispatchSender dispatchSender)
    {
        _underlyingMessagePublisher = underlyingMessagePublisher;
        _dispatchSender = dispatchSender;
    }

    public async Task Publish<TMessagePayload>(TMessagePayload messageDto)
    {
        if (messageDto == null)
        {
            throw new ArgumentNullException(nameof(messageDto));
        }

        await _dispatchSender.SendDispatch(messageDto!);
    }

    public async Task Publish<TMessagePayload>(TMessagePayload messageDto, string sessionId)
    {
        if (messageDto == null)
        {
            throw new ArgumentNullException(nameof(messageDto));
        }

        if (sessionId == null)
        {
            throw new ArgumentNullException(nameof(sessionId));
        }

        await _dispatchSender.SendDispatch(new Abstractions.Dispatch(messageDto)
        {
            SessionId = sessionId,
            DiagnosticId = Activity.Current?.Id
        });
    }

    public async Task Publish<TMessagePayload>(TMessagePayload messageDto, Action<IDispatchContext> messageContextConfiguration)
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

        var dispatch = new Abstractions.Dispatch(messageDto, context)
        {
            SessionId = context.SessionId,
            CorrelationId = context.CorrelationId,
            MessageId = context.MessageId,
            DiagnosticId = context.DiagnosticId ?? Activity.Current?.Id
        };
        await _dispatchSender.SendDispatch(dispatch);
    }
}
