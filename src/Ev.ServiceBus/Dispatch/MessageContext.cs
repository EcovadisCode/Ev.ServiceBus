using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Dispatch;

internal class MessageContext : IMessageContext
{
    public string? CorrelationId { get; set; }
    public string? SessionId { get; set; }
    public string? MessageId { get; set; }
    /// <summary>
    /// Unique identifier of call from producer to the queue or topic. Refer to W3C Trace-Context traceparent header for the format
    /// </summary>
    public string? DiagnosticId { get; set; }
}