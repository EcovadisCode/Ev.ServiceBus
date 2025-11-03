using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Dispatch;

public class DispatchSender : IDispatchSender
{
    private readonly ServiceBusMessageFactory _messageFactory;
    private readonly ServiceBusMessageSender _serviceBusMessageSender;

    public DispatchSender(
        ServiceBusMessageFactory messageFactory,
        ServiceBusMessageSender serviceBusMessageSender)
    {
        _messageFactory = messageFactory;
        _serviceBusMessageSender = serviceBusMessageSender;
    }

    /// <inheritdoc />
    public async Task SendDispatch(object messagePayload, CancellationToken token = default)
    {
        var dispatch = new Abstractions.Dispatch(messagePayload);

        await SendDispatch(dispatch, token);
    }

    /// <inheritdoc />
    public async Task SendDispatch(Abstractions.Dispatch messagePayload, CancellationToken token = default)
    {
        var dispatches = _messageFactory.CreateMessagesToSend([messagePayload]);

        var messagePerResource = dispatches.Single();

        await _serviceBusMessageSender.SendMessages(
            messagePerResource.ResourceId,
            messagePerResource.Messages,
            token);
    }

    /// <inheritdoc />
    public async Task SendDispatches(IEnumerable<object> messagePayloads, CancellationToken token = default)
    {
        if (messagePayloads == null)
        {
            throw new ArgumentNullException(nameof(messagePayloads));
        }

        var dispatches = messagePayloads.Select(o => new Abstractions.Dispatch(o)).ToArray();
        await SendDispatches(dispatches, token);
    }

    /// <inheritdoc />
    public async Task SendDispatches(IEnumerable<Abstractions.Dispatch> messagePayloads, CancellationToken token = default)
    {
        if (messagePayloads == null)
        {
            throw new ArgumentNullException(nameof(messagePayloads));
        }

        var dispatches = _messageFactory.CreateMessagesToSend(messagePayloads);
        foreach (var messagesPerResource in dispatches)
        {
            await _serviceBusMessageSender.SendMessages(messagesPerResource.ResourceId, messagesPerResource.Messages, token);
        }
    }

    /// <inheritdoc />
    public async Task ScheduleDispatches(IEnumerable<object> messagePayloads, DateTimeOffset scheduledEnqueueTime, CancellationToken token = default)
    {
        if (messagePayloads == null)
        {
            throw new ArgumentNullException(nameof(messagePayloads));
        }

        var dispatches = messagePayloads.Select(o => new Abstractions.Dispatch(o)).ToArray();
        await ScheduleDispatches(dispatches, scheduledEnqueueTime, token);
    }

    /// <inheritdoc />
    public async Task ScheduleDispatches(IEnumerable<Abstractions.Dispatch> messagePayloads, DateTimeOffset scheduledEnqueueTime, CancellationToken token = default)
    {
        if (messagePayloads == null)
        {
            throw new ArgumentNullException(nameof(messagePayloads));
        }

        var dispatches = _messageFactory.CreateMessagesToSend(messagePayloads);
        foreach (var messagesPerResource in dispatches)
        {
            await _serviceBusMessageSender.ScheduleMessages(messagesPerResource.ResourceId, messagesPerResource.Messages, scheduledEnqueueTime, token);
        }
    }
}