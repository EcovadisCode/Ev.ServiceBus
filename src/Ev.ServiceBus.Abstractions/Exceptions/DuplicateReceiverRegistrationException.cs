using System;

namespace Ev.ServiceBus.Abstractions;

[Serializable]
public class DuplicateReceiverRegistrationException : Exception
{
    public DuplicateReceiverRegistrationException(string[] clientIds)
    {
        Message = $"Registration of resources failed because some receivers have been registered twice.\n"
                  + $"Incriminated client ids :\n{string.Join("\n", clientIds)}";
    }
    public override string Message { get; }
}