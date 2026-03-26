using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ev.ServiceBus.Abstractions;

public sealed class Dispatch
{
    public Dispatch(object payload)
    {
        Payload = payload;
        ApplicationProperties = new Dictionary<string, object>();
    }

    public object Payload { get; }
    public string? SessionId { get; set; }
    public string? CorrelationId { get; set; }
    public string? MessageId { get; set; }
    public string? DiagnosticId { get; set; }
    public string? PartitionKey { get; set; }
    public string? TransactionPartitionKey { get; set; }
    public string? ReplyToSessionId { get; set; }
    public TimeSpan? TimeToLive { get; set; }
    public string? Subject { get; set; }
    public string? To { get; set; }
    public string? ReplyTo { get; set; }
    public DateTimeOffset? ScheduledEnqueueTime { get; set; }
    public IDictionary<string,object> ApplicationProperties { get; }
}
