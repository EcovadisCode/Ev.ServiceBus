using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Dispatch;

public interface IServiceBusMessageSender
{
    Task SendMessages(string resourceId, ServiceBusMessage[] messages, CancellationToken token);

    Task ScheduleMessages(
        string resourceId,
        ServiceBusMessage[] messages,
        DateTimeOffset scheduledEnqueueTime,
        CancellationToken token);
}