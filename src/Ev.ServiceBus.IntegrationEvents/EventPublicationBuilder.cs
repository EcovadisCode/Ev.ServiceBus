using System;
using System.Collections.Generic;
using System.Linq;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.IntegrationEvents.Publication;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace Ev.ServiceBus.IntegrationEvents
{
    public class EventPublicationBuilder<TIntegrationEvent>
    {
        private readonly List<Action<Message, object>> _outgoingCustomizers;

        private readonly List<Sender> _senders;

        public EventPublicationBuilder()
        {
            _senders = new List<Sender>();
            _outgoingCustomizers = new List<Action<Message, object>>();
        }

        public string? EventTypeId { get; set; }

        public EventPublicationBuilder<TIntegrationEvent> SendToQueue(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            if (_senders.Where(o => o is ServiceBusSender)
                .Cast<ServiceBusSender>()
                .Any(o => o.SenderType == ClientType.Queue && o.SenderName == queueName))
            {
                throw new MultipleServiceBusPublicationRegistrationException(ClientType.Queue, queueName);
            }

            _senders.Add(new ServiceBusSender(this, queueName, ClientType.Queue, _outgoingCustomizers));
            return this;
        }

        public EventPublicationBuilder<TIntegrationEvent> SendToTopic(string topicName)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentNullException(nameof(topicName));
            }

            if (_senders.Where(o => o is ServiceBusSender)
                .Cast<ServiceBusSender>()
                .Any(o => o.SenderType == ClientType.Topic && o.SenderName == topicName))
            {
                throw new MultipleServiceBusPublicationRegistrationException(ClientType.Topic, topicName);
            }

            _senders.Add(new ServiceBusSender(this, topicName, ClientType.Topic, _outgoingCustomizers));
            return this;
        }

        public EventPublicationBuilder<TIntegrationEvent> CustomizeOutgoingMessage(
            Action<Message, object> messageCustomizer)
        {
            _outgoingCustomizers.Add(messageCustomizer);
            return this;
        }

        internal void Build(IServiceCollection services)
        {
            if (string.IsNullOrEmpty(EventTypeId))
            {
                throw new EventTypeIdMustBeSetException();
            }

            foreach (var sender in _senders)
            {
                var publication = sender.Build(services);
                // ReSharper disable once RedundantTypeArgumentsOfMethod
                services.AddSingleton<EventPublicationRegistration>(publication);
            }
        }

        private abstract class Sender
        {
            protected readonly EventPublicationBuilder<TIntegrationEvent> Parent;

            protected Sender(EventPublicationBuilder<TIntegrationEvent> parent)
            {
                Parent = parent;
            }

            public abstract EventPublicationRegistration Build(IServiceCollection services);
        }

        private class ServiceBusSender : Sender
        {
            public ServiceBusSender(
                EventPublicationBuilder<TIntegrationEvent> parent,
                string senderName,
                ClientType senderType,
                IList<Action<Message, object>> outgoingCustomizers)
                : base(parent)
            {
                SenderName = senderName;
                SenderType = senderType;
                OutgoingMessagesCustomizers = outgoingCustomizers;
            }

            public string SenderName { get; }
            public ClientType SenderType { get; }

            public IList<Action<Message, object>> OutgoingMessagesCustomizers { get; }

            public override EventPublicationRegistration Build(IServiceCollection services)
            {
                var serviceBusEventPublicationRegistration = new ServiceBusEventPublicationRegistration(
                    Parent.EventTypeId!,
                    typeof(TIntegrationEvent),
                    SenderType,
                    SenderName,
                    OutgoingMessagesCustomizers);

                return serviceBusEventPublicationRegistration;
            }
        }
    }
}
