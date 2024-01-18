using System;

namespace Ev.ServiceBus.Abstractions;

public class DispatchRegistrationNotFoundException : Exception
{
    public DispatchRegistrationNotFoundException(Type messageType)
    {
        Message = $"The message Type '{messageType}' you tried to retrieve was not found. "
                  + "This means there is most probably a problem with configuration.";
    }

    public override string Message { get; }
}