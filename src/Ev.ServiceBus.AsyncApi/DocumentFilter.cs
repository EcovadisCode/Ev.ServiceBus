using System;
using System.Collections.Generic;
using System.Linq;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Namotion.Reflection;
using NJsonSchema;
using Saunter.AsyncApiSchema.v2;
using Saunter.AsyncApiSchema.v2.Bindings;
using Saunter.AsyncApiSchema.v2.Bindings.Amqp;
using Saunter.AsyncApiSchema.v2.Traits;
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
                ProcessReception(reception, document, context);
            }
        }

        private void ProcessReception(MessageReceptionRegistration reception, AsyncApiDocument document, DocumentFilterContext context)
        {
            var channelName = reception.Options.OriginalResourceId + "/" + reception.PayloadTypeId;
            var channel = GetOrCreateChannel(document, channelName);

            JsonSchema schema = null;
            if (context.SchemaResolver.Schemas.Any(o => o.Title == reception.PayloadType.Name))
            {
                schema = context.SchemaResolver.GetSchema(reception.PayloadType, reception.PayloadType.IsEnum);
            }
            else
            {
                schema = context.SchemaGenerator.Generate(reception.PayloadType);
                context.SchemaResolver.AddSchema(reception.PayloadType, reception.PayloadType.IsEnum, schema);
            }


            channel.Subscribe = new Operation()
            {
                OperationId = reception.PayloadTypeId,
                Bindings = new OperationBindings()
                {
                    Amqp = new AmqpOperationBinding()
                    {
                    }
                },
                Message = new Saunter.AsyncApiSchema.v2.Message()
                {
                     Description = "",
                     Examples = new List<MessageExample>()
                     {
                         new MessageExample()
                         {
                             Payload = context.SchemaGenerator.GenerateExample(reception.PayloadType.ToContextualType())
                         }
                     },
                     Name = "",
                     Payload = schema,
                     Headers = new JsonSchema(),
                     Bindings = new MessageBindings()
                     {
                         Amqp = new AmqpMessageBinding()
                         {
                         }
                     },
                     Summary = "",
                     ContentType = "application/json",
                     Title = ""
                },
                Description = "",
                Summary = "",
                Tags = new HashSet<Tag>(),
                Traits = new List<IOperationTrait>(),
                ExternalDocs = new ExternalDocumentation("")
            };
        }

        private void ProcessDispatch(MessageDispatchRegistration dispatch, AsyncApiDocument document)
        {
        }

        private void ProcessSubscriptionReceiver(SubscriptionOptions options, AsyncApiDocument document)
        {
            ProcessConnectionSettings(options.ConnectionSettings, document);
            var channel = GetOrCreateChannel(document, $"{options.TopicName}/{options.SubscriptionName}");
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

            if (endpoint == null || document.Servers.Any(o => o.Value.Url == endpoint))
            {
                return;
            }

            document.Servers.Add(endpoint, new Server(endpoint, "amqp"));
        }
    }
}
