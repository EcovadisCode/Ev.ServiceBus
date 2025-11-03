using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Diagnostics;
using Ev.ServiceBus.Management;

namespace Ev.ServiceBus.Dispatch;

public class ServiceBusMessageSender : IServiceBusMessageSender
{
    private const int MaxMessagePerSend = 100;
    private readonly ServiceBusRegistry _registry;

    public ServiceBusMessageSender(ServiceBusRegistry registry)
    {
        _registry = registry;
    }

    public async Task SendMessages(string resourceId, ServiceBusMessage[] messages, CancellationToken token)
    {
        var sender = _registry.GetMessageSender(resourceId);
        var batches = await GetBatches(sender, messages, token);

        await Parallel.ForEachAsync(batches,
            new ParallelOptions { CancellationToken = token },
            async (batch, ct) =>
            {
                await sender.SendMessagesAsync(batch, ct);
                batch.Dispose();
            });
    }

    private async Task<ServiceBusMessageBatch[]> GetBatches(
        IMessageSender sender,
        ServiceBusMessage[] messages,
        CancellationToken token)
    {
        var batches = new List<ServiceBusMessageBatch>();
        var batch = await sender.CreateMessageBatchAsync(token);
        batches.Add(batch);
        foreach (var message in messages)
        {
            ServiceBusMeter.IncrementSentCounter(
                1,
                sender.ClientType.ToString(),
                sender.Name,
                message.ApplicationProperties[UserProperties.PayloadTypeIdProperty]?.ToString()
            );

            if (batch.TryAddMessage(message))
            {
                continue;
            }
            batch = await sender.CreateMessageBatchAsync(token);
            batches.Add(batch);
            if (batch.TryAddMessage(message))
            {
                continue;
            }

            throw new ArgumentOutOfRangeException("A message is too big to fit in a single batch");
        }

        return batches.ToArray();
    }

    public async Task ScheduleMessages(
        string resourceId,
        ServiceBusMessage[] messages,
        DateTimeOffset scheduledEnqueueTime,
        CancellationToken token)
    {
        var sender = _registry.GetMessageSender(resourceId);

        var pages = messages
            .Select((x, i) => new
            {
                Item = x,
                Index = i
            })
            .GroupBy(x => x.Index / MaxMessagePerSend, x => x.Item)
            .Select(o => o.ToArray())
            .ToArray();

        await Parallel.ForEachAsync(pages,
            new ParallelOptions { CancellationToken = token },
            async (page, ct) =>
            {
                foreach (var message in page)
                {
                    ServiceBusMeter.IncrementSentCounter(
                        1,
                        sender.ClientType.ToString(),
                        sender.Name,
                        message.ApplicationProperties[UserProperties.PayloadTypeIdProperty]?.ToString()
                    );
                }
                await sender.ScheduleMessagesAsync(page, scheduledEnqueueTime, ct);
            });
    }
}