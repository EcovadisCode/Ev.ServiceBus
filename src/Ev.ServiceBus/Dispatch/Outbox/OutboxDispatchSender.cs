using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Dispatch.Outbox;

public class OutboxDispatchSender : IDispatchSender
{
    private readonly IDispatchSender _underlyingDispatchSender;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ServiceBusMessageFactory _messageFactory;

    public OutboxDispatchSender(
        IDispatchSender underlyingDispatchSender,
        IOutboxRepository outboxRepository,
        ServiceBusMessageFactory serviceBusMessageFactory)
    {
        _underlyingDispatchSender = underlyingDispatchSender;
        _outboxRepository = outboxRepository;
        _messageFactory = serviceBusMessageFactory;
    }

    public async Task SendDispatch(object messagePayload, CancellationToken token = default)
    {
        await SendDispatch(new Abstractions.Dispatch(messagePayload), token);
    }

    public async Task SendDispatch(Abstractions.Dispatch messagePayload, CancellationToken token = default)
    {
        var messagesPerResources = _messageFactory.CreateMessagesToSend([messagePayload]);

        foreach (var messagesPerResource in messagesPerResources)
        {
            foreach (var message in messagesPerResource.Messages)
            {
                await _outboxRepository.Add(messagesPerResource.ResourceId, message, token);
            }
        }
    }

    public async Task SendDispatches(IEnumerable<object> messagePayloads, CancellationToken token = default)
    {
        if (messagePayloads == null)
        {
            throw new ArgumentNullException(nameof(messagePayloads));
        }

        var dispatches = messagePayloads.Select(o => new Abstractions.Dispatch(o)).ToArray();
        await SendDispatches(dispatches, token);
    }

    public async Task SendDispatches(IEnumerable<Abstractions.Dispatch> messagePayloads, CancellationToken token = default)
    {
        var messagesPerResources = _messageFactory.CreateMessagesToSend(messagePayloads);

        foreach (var messagesPerResource in messagesPerResources)
        {
            foreach (var message in messagesPerResource.Messages)
            {
                await _outboxRepository.Add(messagesPerResource.ResourceId, message, token);
            }
        }
    }

    public async Task ScheduleDispatches(IEnumerable<object> messagePayloads, DateTimeOffset scheduledEnqueueTime,
        CancellationToken token = default)
    {
        if (messagePayloads == null)
        {
            throw new ArgumentNullException(nameof(messagePayloads));
        }

        var dispatches = messagePayloads.Select(o => new Abstractions.Dispatch(o)).ToArray();
        await ScheduleDispatches(dispatches, scheduledEnqueueTime, token);
    }

    public async Task ScheduleDispatches(
        IEnumerable<Abstractions.Dispatch> messagePayloads,
        DateTimeOffset scheduledEnqueueTime,
        CancellationToken token = default)
    {
        var messagesPerResources = _messageFactory.CreateMessagesToSend(messagePayloads);

        foreach (var messagesPerResource in messagesPerResources)
        {
            foreach (var message in messagesPerResource.Messages)
            {
                await _outboxRepository.AddScheduled(messagesPerResource.ResourceId, scheduledEnqueueTime, message, token);
            }
        }
    }
}