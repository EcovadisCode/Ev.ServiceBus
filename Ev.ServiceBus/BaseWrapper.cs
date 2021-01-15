using System;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus
{
    public abstract class BaseWrapper
    {
        protected readonly ServiceBusOptions ParentOptions;
        protected readonly IServiceProvider Provider;
        internal MessageReceiver? Receiver;
        internal IMessageSender Sender;

        private readonly ILogger<BaseWrapper> _logger;
        protected BaseWrapper(
            IClientOptions options,
            ServiceBusOptions parentOptions,
            IServiceProvider provider)
        {
            Options = options;
            ParentOptions = parentOptions;
            Provider = provider;
            _logger = Provider.GetRequiredService<ILogger<BaseWrapper>>();
            Sender = new UnavailableSender(Options.EntityPath, options.ClientType);
        }

        internal IClientOptions Options { get; }

        protected abstract (IMessageSender, MessageReceiver?) CreateClient(ConnectionSettings settings);

        public void Initialize()
        {
            _logger.LogInformation($"[Ev.ServiceBus] Initialization of client '{Options.EntityPath}': Start.");
            if (ParentOptions.Settings.Enabled == false)
            {
                if (Options.ClientType != ClientType.Subscription)
                {
                    Sender = new DeactivatedSender(Options.EntityPath, Options.ClientType);
                }
                Receiver = null;
                _logger.LogInformation($"[Ev.ServiceBus] Initialization of client '{Options.EntityPath}': Client deactivated through configuration.");
                return;
            }
            try
            {
                var resolver = Options.ConnectionSettings ?? ParentOptions.Settings.ConnectionSettings;
                if (resolver == null)
                {
                    throw new MissingConnectionException(Options, ClientType.Topic);
                }
                (Sender, Receiver) = CreateClient(resolver);
                _logger.LogInformation($"[Ev.ServiceBus] Initialization of client '{Options.EntityPath}': Success.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Ev.ServiceBus] Initialization of client '{Options.EntityPath}': Failed.");
            }
        }
    }
}
