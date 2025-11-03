using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Dispatch.Outbox;

public interface IOutboxService
{
    Task StoreMessage(string resourceId, ServiceBusMessage message, CancellationToken token);
    Task StoreScheduledMessage(string resourceId, DateTimeOffset scheduledEnqueueTime, ServiceBusMessage message, CancellationToken token);
    Task EagerlySendStoredMessages(CancellationToken token);
}
