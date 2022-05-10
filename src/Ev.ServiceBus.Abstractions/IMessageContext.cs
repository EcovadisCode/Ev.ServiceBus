namespace Ev.ServiceBus.Abstractions;

public interface IMessageContext
{
    public string? SessionId { get; set; }
    public string? CorrelationId { get; set; }
}