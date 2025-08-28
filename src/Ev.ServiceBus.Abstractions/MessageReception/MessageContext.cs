using System.Threading;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions.Extensions;
using Ev.ServiceBus.Abstractions.MessageReception;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions;

public class MessageContext
{
    public MessageContext(ProcessSessionMessageEventArgs args, ClientType clientType, string resourceId)
    {
        SessionArgs = args;
        ClientType = clientType;
        ResourceId = resourceId;
        Message = args.Message;
        CancellationToken = args.CancellationToken;
        PayloadTypeId = Message.GetPayloadTypeId();
        IsolationKey = Message.GetIsolationKey();
        IsolationApps = Message.GetIsolationApps();
    }

    public MessageContext(ProcessMessageEventArgs args, ClientType clientType, string resourceId)
    {
        Args = args;
        ClientType = clientType;
        ResourceId = resourceId;
        Message = args.Message;
        CancellationToken = args.CancellationToken;
        PayloadTypeId = Message.GetPayloadTypeId();
        IsolationKey = Message.GetIsolationKey();
        IsolationApps = Message.GetIsolationApps();
    }

    public ServiceBusReceivedMessage Message { get; }
    public CancellationToken CancellationToken { get; }
    public ProcessSessionMessageEventArgs? SessionArgs { get; }
    public ClientType ClientType { get; }
    public string ResourceId { get; }
    public ProcessMessageEventArgs? Args { get; }

    public string? PayloadTypeId { get; internal set; }
    public MessageReceptionRegistration? ReceptionRegistration { get; internal set; }
    public string? IsolationKey { get; internal set; }
    public string[] IsolationApps { get; internal set; }

    public MessageExecutionContext ReadExecutionContext()
    {
        var clientType = GetClientType();
        var contextResourceId = GetContextResourceId();
        var messageMessageId = GetMessageId();
        var contextPayloadTypeId = GetPayloadTypeId();
        var sessionArgsSessionId = GetSessionId();
        var handlerTypeFullName = GetHandlerTypeFullName();

        return new MessageExecutionContext
        {
            ClientType = clientType,
            ResourceId = contextResourceId,
            MessageId = messageMessageId,
            PayloadTypeId = contextPayloadTypeId,
            SessionId = sessionArgsSessionId,
            HandlerName = handlerTypeFullName,
            DiagnosticId = Message.GetDiagnosticId(),
            IsolationKey = IsolationKey,
        };
    }

    private string GetHandlerTypeFullName()
        => ReceptionRegistration?.HandlerType.FullName ?? "none";

    private string GetSessionId()
        => SessionArgs?.SessionId ?? "none";

    private string GetPayloadTypeId()
        => PayloadTypeId ?? "none";

    private string GetMessageId()
        => Message.MessageId;

    private string GetContextResourceId()
        => ResourceId;

    private string GetClientType()
        => ClientType.ToString();
}