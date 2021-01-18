using System;

namespace Ev.ServiceBus.Abstractions
{
    [Serializable]
    public class MissingConnectionException : Exception
    {
        public MissingConnectionException(IClientOptions options, ClientType clientType)
        {
            Message = $"The {clientType} client '{options.EntityPath}' is missing connection information.";
        }

        public override string Message { get; }
    }
}
