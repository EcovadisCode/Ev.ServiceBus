using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ev.ServiceBus")]
[assembly: InternalsVisibleTo("Ev.ServiceBus.IntegrationEvents")]

namespace Ev.ServiceBus.Abstractions
{
    public class ServiceBusOptions
    {
        private readonly List<ClientOptions> _senders;
        private readonly List<ReceiverOptions> _receivers;

        public ServiceBusOptions()
        {
            Settings = new ServiceBusSettings();
            _senders = new List<ClientOptions>();
            Senders = new ReadOnlyCollection<ClientOptions>(_senders);
            _receivers = new List<ReceiverOptions>();
            Receivers = new ReadOnlyCollection<ReceiverOptions>(_receivers);
        }

        public ServiceBusSettings Settings { get; }

        public ReadOnlyCollection<ClientOptions> Senders { get; }
        public ReadOnlyCollection<ReceiverOptions> Receivers { get; }

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
    }
}
