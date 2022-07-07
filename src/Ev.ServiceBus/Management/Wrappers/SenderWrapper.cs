using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IMessageSender = Ev.ServiceBus.Abstractions.IMessageSender;

namespace Ev.ServiceBus
{
    public class SenderWrapper : IWrapper
    {
        private readonly ConnectionSettings? _connectionSettings;
        private readonly ILogger<SenderWrapper> _logger;
        private readonly ServiceBusClient? _client;
        private readonly ServiceBusOptions _parentOptions;
        private readonly IServiceProvider _provider;

        public SenderWrapper(ServiceBusClient? client,
            IClientOptions[] options,
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
            _client = client;
            _parentOptions = parentOptions;
            _provider = provider;
            _logger = _provider.GetRequiredService<ILogger<SenderWrapper>>();
            Sender = new UnavailableSender(ResourceId, ClientType);
        }

        public string ResourceId { get; }
        public ClientType ClientType { get; }
        internal IMessageSender Sender { get; private set; }
        private IClientOptions[] Options { get; }

        internal ServiceBusSender? SenderClient { get; private set; }

        public void Initialize()
        {
            SenderClient = _client!.CreateSender(ResourceId);
            Sender = new MessageSender(SenderClient, ResourceId, ClientType, _provider.GetRequiredService<ILogger<MessageSender>>());
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            if (SenderClient == null)
            {
                return;
            }

            if (SenderClient.IsClosed)
            {
                return;
            }

            try
            {
                await SenderClient.CloseAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Ev.ServiceBus] Client {ResourceId} couldn't close properly");
            }
        }
    }
}
