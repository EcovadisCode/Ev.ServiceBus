using System.Collections.Generic;
using System.Linq;

namespace Ev.ServiceBus.Abstractions;

public abstract class ExecutionBaseArgs
{
    protected ExecutionBaseArgs(MessageContext context)
    {
        ClientType = context.ClientType;
        ResourceId = context.ResourceId;
        MessageLabel = context.Message.Subject;
        MessageApplicationProperties = context.Message.ApplicationProperties.ToDictionary(pair => pair.Key, pair => pair.Value);
        ReceptionRegistration = context.ReceptionRegistration;
        MessageBody = context.Message.Body.ToArray();
        MessageContentType = context.Message.ContentType;
    }

    public MessageReceptionRegistration? ReceptionRegistration { get; }
    public ClientType ClientType { get; }
    public string ResourceId { get; }
    public IDictionary<string, object> MessageApplicationProperties { get; }
    public string MessageLabel { get; }
    public byte[] MessageBody { get; }
    public string MessageContentType { get; }
}