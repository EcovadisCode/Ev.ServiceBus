using System;
using System.Linq;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IMessageSender = Ev.ServiceBus.Abstractions.IMessageSender;

namespace Ev.ServiceBus
{
    public class SenderWrapper
    {
        private readonly ConnectionSettings? _connectionSettings;
        private readonly ILogger<SenderWrapper> _logger;
        private readonly ServiceBusOptions _parentOptions;
        private readonly IServiceProvider _provider;

        public SenderWrapper(IClientOptions[] options,
            ServiceBusOptions parentOptions,
            IServiceProvider provider)
        {
            if (options.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(options));
            }

            ResourceId = options.First().ResourceId;
            ClientType = options.First().ClientType;
            _connectionSettings = options.First().ConnectionSettings;
            Options = options;
            _parentOptions = parentOptions;
            _provider = provider;
            _logger = _provider.GetRequiredService<ILogger<SenderWrapper>>();
            Sender = new UnavailableSender(ResourceId, ClientType);
        }

        public string ResourceId { get; }
        public ClientType ClientType { get; }

        internal IMessageSender Sender { get; private set; }
        private IClientOptions[] Options { get; }

        internal ISenderClient? SenderClient { get; private set; }

        public void Initialize()
        {
            _logger.LogInformation($"[Ev.ServiceBus] Initialization of sender client '{ResourceId}': Start.");
            if (_parentOptions.Settings.Enabled == false)
            {
                Sender = new DeactivatedSender(ResourceId, ClientType);

                _logger.LogInformation(
                    $"[Ev.ServiceBus] Initialization of sender client '{ResourceId}': Client deactivated through configuration.");
                return;
            }

            try
            {
                var connectionSettings = _connectionSettings ?? _parentOptions.Settings.ConnectionSettings;
                if (connectionSettings == null)
                {
                    throw new MissingConnectionException(ResourceId, ClientType.Topic);
                }

                switch (ClientType)
                {
                    case ClientType.Queue:
                        CreateQueueClient(connectionSettings);
                        break;
                    case ClientType.Topic:
                        CreateTopicClient(connectionSettings);
                        break;
                }

                _logger.LogInformation($"[Ev.ServiceBus] Initialization of sender client '{ResourceId}': Success.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"[Ev.ServiceBus] Initialization of sender client '{ResourceId}': Failed.");
            }
        }

        private void CreateTopicClient(ConnectionSettings connectionSettings)
        {
            var factory = _provider.GetService<IClientFactory<TopicOptions, ITopicClient>>();
            SenderClient = factory.Create((TopicOptions) Options.First(), connectionSettings);
            Sender = new MessageSender(SenderClient, ResourceId, ClientType);
        }

        private void CreateQueueClient(ConnectionSettings connectionSettings)
        {
            var factory = _provider.GetService<IClientFactory<QueueOptions, IQueueClient>>();
            SenderClient = factory.Create((QueueOptions) Options.First(), connectionSettings);
            Sender = new MessageSender(SenderClient, ResourceId, ClientType);
        }
    }
}
