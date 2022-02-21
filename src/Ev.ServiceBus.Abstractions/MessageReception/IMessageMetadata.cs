using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Abstractions.MessageReception;

public interface IMessageMetadata
{
    public string ContentType { get; }
    public string CorrelationId { get; }
    public string Label { get; }
    public string SessionId { get; }
    public CancellationToken CancellationToken { get; }
    public IReadOnlyDictionary<string, object> ApplicationProperties { get; }
}

public class MessageMetadata : IMessageMetadata
{
    public MessageMetadata(ServiceBusReceivedMessage message, CancellationToken token)
    {
        CorrelationId = message.CorrelationId;
        ContentType = message.ContentType;
        Label = message.Subject;
        SessionId = message.SessionId;
        CancellationToken = token;
        ApplicationProperties = message.ApplicationProperties.ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    public string ContentType { get; }
    public string CorrelationId { get; }
    public string Label { get; }
    public string SessionId { get; }
    public CancellationToken CancellationToken { get; }
    public IReadOnlyDictionary<string, object> ApplicationProperties { get; }
}