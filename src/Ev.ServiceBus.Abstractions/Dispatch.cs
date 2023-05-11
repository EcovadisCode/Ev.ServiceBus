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

    public Dispatch(object payload, IDispatchContext context)
    {
        SessionId = context.SessionId;
        CorrelationId = context.CorrelationId;
        MessageId = context.MessageId;
        DiagnosticId = context.DiagnosticId ?? Activity.Current?.Id;
        ApplicationProperties = new Dictionary<string, object>(context.ApplicationProperties);
        Payload = payload;
    }

    public object Payload { get; }
    public string? SessionId { get; set; }
    public string? CorrelationId { get; set; }
    public string? MessageId { get; set; }
    public string? DiagnosticId { get; set; }
    public IDictionary<string,object> ApplicationProperties { get; }
}

