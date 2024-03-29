using System.Collections.Generic;

namespace Ev.ServiceBus.Abstractions;

public interface IDispatchContext
{
    public string? SessionId { get; set; }
    public string? CorrelationId { get; set; }
    public string? MessageId { get; set; }
    /// <summary>
    /// Unique identifier of call from producer to the queue or topic. Refer to W3C Trace-Context traceparent header for the format
    /// </summary>
    public string? DiagnosticId { get; set; }
    IDictionary<string, object> ApplicationProperties { get; }
}
