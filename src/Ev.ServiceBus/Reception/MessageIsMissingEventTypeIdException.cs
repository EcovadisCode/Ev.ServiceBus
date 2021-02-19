using System;
using System.Linq;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Reception
{
    public class MessageIsMissingEventTypeIdException : Exception
    {
        public MessageIsMissingEventTypeIdException(MessageContext messageContext)
        {
            Message = "An incoming message is missing its 'EventTypeId' UserProperty. Processing cannot continue without this information.\n"
                      + $"context : \n"
                      + $"\tReceiver : {messageContext.Receiver.ClientType} {messageContext.Receiver.Name}"
                      + $"\tLabel : {messageContext.Message.Label}"
                      + $"\tUser properties : \n"
                      + $"{string.Join("\n", messageContext.Message.UserProperties.Select(o => $"\t\t{o.Key} : {o.Value}"))}";
        }

        public override string Message { get; }
    }
}
