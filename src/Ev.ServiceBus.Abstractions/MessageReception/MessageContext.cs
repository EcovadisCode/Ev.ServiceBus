using System.Threading;
using Azure.Messaging.ServiceBus;

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
    }

    public MessageContext(ProcessMessageEventArgs args, ClientType clientType, string resourceId)
    {
        Args = args;
        ClientType = clientType;
        ResourceId = resourceId;
        Message = args.Message;
        CancellationToken = args.CancellationToken;
    }

    public ServiceBusReceivedMessage Message { get; }
    public CancellationToken CancellationToken { get; }
    public ProcessSessionMessageEventArgs? SessionArgs { get; }
    public ClientType ClientType { get; }
    public string ResourceId { get; }
    public ProcessMessageEventArgs? Args { get; }
}