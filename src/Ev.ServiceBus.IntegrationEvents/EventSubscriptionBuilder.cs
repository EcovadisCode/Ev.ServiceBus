using System;
using System.Collections.Generic;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.IntegrationEvents.Subscription;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace Ev.ServiceBus.IntegrationEvents
{
    public class EventSubscriptionBuilder<TIntegrationEvent, THandler>
        where THandler : class, IIntegrationEventHandler<TIntegrationEvent>
    {
        private readonly List<Receiver> _receivers;

        public EventSubscriptionBuilder()
        {
            _receivers = new List<Receiver>();
        }

        public string? EventTypeId { get; set; }

        public EventSubscriptionBuilder<TIntegrationEvent, THandler> ReceiveFromQueue(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            _receivers.Add(new ServiceBusReceiver(this, queueName, ClientType.Queue));
            return this;
        }

        public EventSubscriptionBuilder<TIntegrationEvent, THandler> ReceiveFromSubscription(string topicName, string subscriptionName)
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                throw new ArgumentNullException(nameof(topicName));
            }

            if (string.IsNullOrWhiteSpace(subscriptionName))
            {
                throw new ArgumentNullException(nameof(subscriptionName));
            }

            var receiverName = EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName);
            _receivers.Add(new ServiceBusReceiver(this, receiverName, ClientType.Subscription));
            return this;
        }

        internal void Build(IServiceCollection services)
        {
            if (string.IsNullOrEmpty(EventTypeId))
            {
                throw new EventTypeIdMustBeSetException();
            }

            foreach (var receiver in _receivers)
            {
                var subscription = receiver.Build(services);

                services.AddScoped<THandler>();
                // ReSharper disable once RedundantTypeArgumentsOfMethod
                services.AddSingleton<EventSubscriptionRegistration>(subscription);
            }
        }

        private abstract class Receiver
        {
            protected readonly EventSubscriptionBuilder<TIntegrationEvent, THandler> Parent;

            protected Receiver(EventSubscriptionBuilder<TIntegrationEvent, THandler> parent)
            {
                Parent = parent;
            }

            public abstract EventSubscriptionRegistration Build(IServiceCollection services);
        }

        private class ServiceBusReceiver : Receiver
        {
            private readonly string _receiverName;
            private readonly ClientType _receiverType;

            public ServiceBusReceiver(
                EventSubscriptionBuilder<TIntegrationEvent, THandler> parent,
                string receiverName,
                ClientType receiverType) : base(parent)
            {
                _receiverName = receiverName;
                _receiverType = receiverType;
            }

            public override EventSubscriptionRegistration Build(IServiceCollection services)
            {
                var registration = new ServiceBusEventSubscriptionRegistration(
                    Parent.EventTypeId!,
                    typeof(TIntegrationEvent),
                    typeof(THandler),
                    _receiverType,
                    _receiverName);
                services.AddSingleton(registration);
                return registration;
            }
        }
    }
}
