using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Abstractions.MessageReception
{
    public interface IMessageMetadata
    {
        public string ContentType { get; }
        public string CorrelationId { get; }
        public string Label { get; }
        public string SessionId { get; }
        public CancellationToken CancellationToken { get; }
        public IReadOnlyDictionary<string, object> UserProperties { get; }
    }

    public class MessageMetadata : IMessageMetadata
    {
        public MessageMetadata(Message message, CancellationToken token)
        {
            CorrelationId = message.CorrelationId;
            ContentType = message.ContentType;
            Label = message.Label;
            SessionId = message.SessionId;
            CancellationToken = token;
            UserProperties = message.UserProperties.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public string ContentType { get; }
        public string CorrelationId { get; }
        public string Label { get; }
        public string SessionId { get; }
        public CancellationToken CancellationToken { get; }
        public IReadOnlyDictionary<string, object> UserProperties { get; }
    }
}
