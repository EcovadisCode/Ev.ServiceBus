using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Ev.ServiceBus.Abstractions.Configuration;

[assembly: InternalsVisibleTo("Ev.ServiceBus")]

namespace Ev.ServiceBus.Abstractions
{
    public class ServiceBusOptions
    {
        private readonly List<ClientOptions> _senders;
        private readonly List<ReceiverOptions> _receivers;
        private readonly List<MessageReceptionRegistration> _receptionRegistrations;
        private readonly List<MessageDispatchRegistration> _dispatchRegistrations;

        public ServiceBusOptions()
        {
            Settings = new ServiceBusSettings();
            _senders = new List<ClientOptions>();
            Senders = new ReadOnlyCollection<ClientOptions>(_senders);
            _receivers = new List<ReceiverOptions>();
            Receivers = new ReadOnlyCollection<ReceiverOptions>(_receivers);
            _receptionRegistrations = new List<MessageReceptionRegistration>();
            ReceptionRegistrations = new ReadOnlyCollection<MessageReceptionRegistration>(_receptionRegistrations);
            _dispatchRegistrations = new List<MessageDispatchRegistration>();
            DispatchRegistrations = new ReadOnlyCollection<MessageDispatchRegistration>(_dispatchRegistrations);
        }

        /// <summary>
        /// General settings for Ev.ServiceBus.
        /// </summary>
        public ServiceBusSettings Settings { get; }

        /// <summary>
        /// The list of registered senders.
        /// </summary>
        public ReadOnlyCollection<ClientOptions> Senders { get; }

        /// <summary>
        /// The list of registered receivers.
        /// </summary>
        public ReadOnlyCollection<ReceiverOptions> Receivers { get; }

        /// <summary>
        /// The list of reception registration.
        /// </summary>
        public ReadOnlyCollection<MessageReceptionRegistration> ReceptionRegistrations { get; }

        /// <summary>
        /// The list of dispatch registrations.
        /// </summary>
        public ReadOnlyCollection<MessageDispatchRegistration> DispatchRegistrations { get; }

        /// <summary>
        ///     Registers a queue that can be used to send or receive messages.
        /// </summary>
        /// <param name="queue">The name of the queue. It must be unique.</param>
        internal void RegisterQueue(QueueOptions queue)
        {
            _senders.Add(queue);
            _receivers.Add(queue);
        }

        /// <summary>
        ///     Registers a topic that can be used to send messages.
        /// </summary>
        /// <param name="topic">The name of the topic. It must be unique.</param>
        internal void RegisterTopic(TopicOptions topic)
        {
            _senders.Add(topic);
        }

        /// <summary>
        ///     Registers a subscription that can be used to receive messages.
        /// </summary>
        /// <param name="subscription">The name of the topic.</param>
        internal void RegisterSubscription(SubscriptionOptions subscription)
        {
            _receivers.Add(subscription);
        }

        internal void RegisterReception(MessageReceptionBuilder reception)
        {
            _receptionRegistrations.Add(reception.Build());
        }

        internal void RegisterDispatch(MessageDispatchRegistration dispatch)
        {
            _dispatchRegistrations.Add(dispatch);
        }
    }
}
