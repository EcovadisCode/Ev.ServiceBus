using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Dispatch;

internal class MessageContext : IMessageContext
{
    public string? CorrelationId { get; set; }
    public string? SessionId { get; set; }
    public string? MessageId { get; set; }
    public string? DiagnosticId { get; set; }
}