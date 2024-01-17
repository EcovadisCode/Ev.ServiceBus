using System;

namespace Ev.ServiceBus.Abstractions;

public class MessageReceptionRegistration
{
    public MessageReceptionRegistration(ClientOptions clientOptions, Type payloadType, Type handlerType)
    {
        Options = clientOptions;
        PayloadType = payloadType;
        HandlerType = handlerType;
        PayloadTypeId = PayloadType.Name;
    }

    /// <summary>
    /// Settings of the underlying resource that will receive the messages.
    /// </summary>
    public ClientOptions Options { get; }

    /// <summary>
    /// The type the receiving message wil be deserialized into.
    /// </summary>
    public Type PayloadType { get; }

    /// <summary>
    /// The class that will be resolved to process the incoming message.
    /// </summary>
    public Type HandlerType { get; }

    /// <summary>
    /// The unique identifier of this payload's type.
    /// </summary>
    public string PayloadTypeId { get; internal set; }
}