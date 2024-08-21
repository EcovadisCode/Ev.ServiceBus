using System;

namespace Ev.ServiceBus.Abstractions.Configuration;

public class MessageReceptionBuilder
{
    private readonly MessageReceptionRegistration _registration;
    public Type HandlerType => _registration.HandlerType;

    public MessageReceptionBuilder(ClientOptions clientOptions, Type payloadType, Type handlerType)
    {
        _registration = new MessageReceptionRegistration(clientOptions, payloadType, handlerType);
    }

    /// <summary>
    /// Sets the PayloadTypeId (by default it will take the <see cref="MemberInfo.Name"/> of the payload <see cref="Type"/> object)
    /// </summary>
    /// <param name="payloadTypeId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public MessageReceptionBuilder CustomizePayloadTypeId(string payloadTypeId)
    {
        if (payloadTypeId == null)
        {
            throw new ArgumentNullException(nameof(payloadTypeId));
        }

        _registration.PayloadTypeId = payloadTypeId;
        return this;
    }

    internal MessageReceptionRegistration Build()
    {
        return _registration;
    }
}
