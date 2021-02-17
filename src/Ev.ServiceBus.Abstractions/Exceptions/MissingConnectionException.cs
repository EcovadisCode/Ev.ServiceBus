using System;

namespace Ev.ServiceBus.Abstractions
{
    [Serializable]
    public class MissingConnectionException : Exception
    {
        public MissingConnectionException(string resourceId, ClientType clientType)
        {
            Message = $"The {clientType} client '{resourceId}' is missing connection information.";
        }

        public override string Message { get; }
    }
}
