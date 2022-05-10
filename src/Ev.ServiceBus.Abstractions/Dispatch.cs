namespace Ev.ServiceBus.Abstractions;

public sealed class Dispatch
{
    public Dispatch(object payload)
    {
        Payload = payload;
    }

    public object Payload { get; }
    public string? SessionId { get; set; }
    public string? CorrelationId { get; set; }
}

