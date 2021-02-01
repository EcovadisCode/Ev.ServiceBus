using System;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.IntegrationEvents
{
    public class MultipleServiceBusPublicationRegistrationException : Exception
    {
        public ClientType ClientType { get; }
        public string TopicName { get; }

        public MultipleServiceBusPublicationRegistrationException(ClientType clientType, string topicName)
            : base($"You cannot register the same sender twice ({clientType}, {topicName})")
        {
            ClientType = clientType;
            TopicName = topicName;
        }
    }
}