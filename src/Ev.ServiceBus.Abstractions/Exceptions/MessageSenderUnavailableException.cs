using System;

namespace Ev.ServiceBus.Abstractions
{
    [Serializable]
    public class MessageSenderUnavailableException : Exception
    {
        public MessageSenderUnavailableException(string name)
            : base($"The sender '{name}' is not available. "
                   + $"This is most likely caused by an error during initialization or a critical failure.")
        {
        }
    }
}
