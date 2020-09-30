using System;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus
{
    public abstract class BaseWrapper
    {
        protected readonly ServiceBusOptions ParentOptions;
        protected readonly IServiceProvider Provider;
        internal MessageReceiver Receiver;
        internal IMessageSender Sender;

        private readonly ILogger<BaseWrapper> _logger;
        private IClientEntity _entity;

        protected BaseWrapper(
            ServiceBusOptions parentOptions,
            IServiceProvider provider,
            string name)
        {
            Name = name;
            ParentOptions = parentOptions;
            Provider = provider;
            _logger = Provider.GetRequiredService<ILogger<BaseWrapper>>();
            Sender = new UnavailableSender(Name);
        }

        public string Name { get; }

        protected abstract (IMessageSender, MessageReceiver, IClientEntity) CreateClient();

        public void Initialize()
        {
            _logger.LogInformation($"Initialization of client '{Name}': Start.");
            try
            {
                (Sender, Receiver, _entity) = CreateClient();
                _logger.LogInformation($"Initialization of client '{Name}': Success.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Initialization of client '{Name}': Failed.");
            }
        }

        protected async Task Close()
        {
            if (_entity != null)
            {
                await _entity.CloseAsync().ConfigureAwait(false);
            }
            Sender = new UnavailableSender(Name);
        }
    }
}
