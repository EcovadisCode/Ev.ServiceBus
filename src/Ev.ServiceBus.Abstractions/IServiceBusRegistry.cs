using System;

namespace Ev.ServiceBus.Abstractions;

public interface IServiceBusRegistry
{
    /// <summary>
    /// Gets the registrations for a specific dispatch contract
    /// </summary>
    /// <param name="messageType"></param>
    /// <returns></returns>
    MessageDispatchRegistration[] GetDispatchRegistrations(Type messageType);

    /// <summary>
    /// Gets the registration for a specific reception contract.
    /// </summary>
    /// <param name="payloadTypeId"></param>
    /// <param name="receiverName"></param>
    /// <param name="clientType"></param>
    /// <returns></returns>
    MessageReceptionRegistration? GetReceptionRegistration(string payloadTypeId, string receiverName, ClientType clientType);
}