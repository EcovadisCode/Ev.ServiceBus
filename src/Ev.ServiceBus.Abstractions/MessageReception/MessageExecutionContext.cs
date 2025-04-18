namespace Ev.ServiceBus.Abstractions.MessageReception;

public class MessageExecutionContext
{
    public string? ClientType { get; set; }
    public string? ResourceId { get; set; }
    public string? PayloadTypeId { get; set; }
    public string? MessageId { get; set; }
    public string? SessionId { get; set; }
    public string? HandlerName { get; set; }

    public string? DiagnosticId { get; set; }
    public string? IsolationKey { get; set; }

    public string ExecutionName => $"{ClientType}/{ResourceId}/{PayloadTypeId}";
}