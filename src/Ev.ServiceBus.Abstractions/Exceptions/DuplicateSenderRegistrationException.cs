using System;

namespace Ev.ServiceBus.Abstractions;

[Serializable]
public class DuplicateSenderRegistrationException : Exception
{
    public DuplicateSenderRegistrationException(string[] clientIds)
    {
        Message = "Registration of resources failed because some senders have been registered twice.\n"
                  + $"Incriminated client ids :\n{string.Join("\n", clientIds)}";
    }

    public override string Message { get; }
}