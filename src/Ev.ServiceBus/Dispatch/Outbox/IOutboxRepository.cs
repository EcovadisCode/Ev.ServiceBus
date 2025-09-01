using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Dispatch.Outbox;

public interface IOutboxRepository
{
    Task Add(string resourceId, ServiceBusMessage message, CancellationToken token);
    Task AddScheduled(string resourceId, DateTimeOffset scheduledEnqueueTime, ServiceBusMessage message, CancellationToken token);
}
