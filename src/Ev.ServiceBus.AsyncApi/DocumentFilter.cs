using System;
using System.Linq;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Saunter.AsyncApiSchema.v2;
using Saunter.AsyncApiSchema.v2.Bindings;
using Saunter.AsyncApiSchema.v2.Bindings.Amqp;
using Saunter.Generation.Filters;

namespace Ev.ServiceBus.AsyncApi
{
    public class DocumentFilter : IDocumentFilter
    {
        private readonly IOptions<ServiceBusOptions> _options;

        public DocumentFilter(IOptions<ServiceBusOptions> options)
        {
            _options = options;
        }

        public void Apply(AsyncApiDocument document, DocumentFilterContext context)
        {
            document.DefaultContentType = "application/json";
            ProcessConnectionSettings(_options.Value.Settings.ConnectionSettings, document);

            foreach (var sender in _options.Value.Senders)
            {
                switch (sender.ClientType)
                {
                    case ClientType.Queue: ProcessQueueSender((QueueOptions)sender, document); break;
                    case ClientType.Topic: ProcessTopicSender((TopicOptions)sender, document); break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }

            foreach (var receiver in _options.Value.Receivers)
            {
                switch (receiver.ClientType)
                {
                    case ClientType.Queue: ProcessQueueReceiver((QueueOptions)receiver, document); break;
                    case ClientType.Subscription: ProcessSubscriptionReceiver((SubscriptionOptions)receiver, document); break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }

            foreach (var dispatch in _options.Value.DispatchRegistrations)
            {
                ProcessDispatch(dispatch, document);
            }

            foreach (var reception in _options.Value.ReceptionRegistrations)
            {
                ProcessReception(reception, document);
            }
        }

        private void ProcessReception(MessageReceptionRegistration reception, AsyncApiDocument document)
        {
        }

        private void ProcessDispatch(MessageDispatchRegistration dispatch, AsyncApiDocument document)
        {
        }

        private void ProcessSubscriptionReceiver(SubscriptionOptions options, AsyncApiDocument document)
        {
            ProcessConnectionSettings(options.ConnectionSettings, document);
            var channel = GetOrCreateChannel(document, $"{options.TopicName}/{options.SubscriptionName}");
            channel.
        }

        private void ProcessQueueReceiver(QueueOptions options, AsyncApiDocument document)
        {
            ProcessConnectionSettings(options.ConnectionSettings, document);
            var channel = GetOrCreateChannel(document, options.QueueName);
        }

        private void ProcessTopicSender(TopicOptions options, AsyncApiDocument document)
        {
            ProcessConnectionSettings(options.ConnectionSettings, document);
            var channel = GetOrCreateChannel(document, options.TopicName);
        }

        private void ProcessQueueSender(QueueOptions options, AsyncApiDocument document)
        {
            ProcessConnectionSettings(options.ConnectionSettings, document);
            var channel = GetOrCreateChannel(document, options.QueueName);
        }

        private ChannelItem GetOrCreateChannel(AsyncApiDocument document, string name)
        {
            if (document.Channels.ContainsKey(name))
            {
                return document.Channels[name];
            }

            var channel = new ChannelItem();
            channel.Bindings = new ChannelBindings()
            {
                Amqp = new AmqpChannelBinding()
                {
                    Is = AmqpChannelBindingIs.Queue,
                    Queue = new AmqpChannelBindingQueue()
                    {
                        Durable = true,
                        Exclusive = false,
                        AutoDelete = false,
                        Name = name
                    }
                }
            };
            document.Channels.Add(name, channel);
            return channel;
        }

        private void ProcessConnectionSettings(ConnectionSettings? connectionSettings, AsyncApiDocument document)
        {
            if (connectionSettings == null)
            {
                return;
            }

            string endpoint;
            if (connectionSettings.Connection != null)
            {
                endpoint = connectionSettings.Connection.Endpoint.ToString();
            }
            else if (connectionSettings.ConnectionStringBuilder != null)
            {
                endpoint = connectionSettings.ConnectionStringBuilder.Endpoint;
            }
            else if (connectionSettings.ConnectionString != null)
            {
                var builder = new ServiceBusConnectionStringBuilder(connectionSettings.ConnectionString);
                endpoint = builder.Endpoint;
            }
            else
            {
                return;
            }

            if (document.Servers.Any(o => o.Value.Url == endpoint))
            {
                return;
            }

            document.Servers.Add(endpoint, new Server(endpoint, "amqp"));
        }
    }
}
