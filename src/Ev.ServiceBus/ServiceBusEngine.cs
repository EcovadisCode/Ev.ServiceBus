using System;
using System.Linq;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.Reception;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ev.ServiceBus
{
    public class ServiceBusEngine
    {
        private readonly ILogger<ServiceBusEngine> _logger;
        private readonly IOptions<ServiceBusOptions> _options;
        private readonly IServiceProvider _provider;
        private readonly ServiceBusRegistry _registry;

        public ServiceBusEngine(ILogger<ServiceBusEngine> logger,
            IOptions<ServiceBusOptions> options,
            ServiceBusRegistry registry,
            IServiceProvider provider)
        {
            _logger = logger;
            _options = options;
            _registry = registry;
            _provider = provider;
        }

        public void StartAll()
        {
            _logger.LogInformation("[Ev.ServiceBus] Starting azure service bus clients");

            if (_options.Value.Settings.Enabled == false)
            {
                _logger.LogInformation("[Ev.ServiceBus] Reception and dispatch of messages have been deactivated through configuration.");
            }
            if (_options.Value.Settings.Enabled && _options.Value.Settings.ReceiveMessages == false)
            {
                _logger.LogInformation("[Ev.ServiceBus] Reception of messages have been deactivated through configuration.");
            }

            BuildSenders();
            BuildReceivers();
            BuildReceptions();
            BuildDispatches();
        }

        private void BuildDispatches()
        {
            var doubleRegistrations = _options.Value.DispatchRegistrations.GroupBy(o => o).Where(o => o.Count() > 1).ToArray();
            if (doubleRegistrations.Any())
            {
                throw new MultiplePublicationRegistrationException(doubleRegistrations.Select(o => o.Key.ToString()).ToArray());
            }

            foreach (var group in _options.Value.DispatchRegistrations.GroupBy(o => o.PayloadType))
            {
                _registry.Register(group.Key, group.ToArray());
            }
        }

        private void BuildReceptions()
        {
            var regs = _options.Value.ReceptionRegistrations.ToArray();

            var duplicatedHandlers = regs.GroupBy(o => new { o.Options.ClientType,
                o.Options.ResourceId, o.HandlerType }).Where(o => o.Count() > 1).ToArray();
            if (duplicatedHandlers.Any())
            {
                throw new DuplicateSubscriptionHandlerDeclarationException(duplicatedHandlers.SelectMany(o => o).ToArray());
            }

            var duplicateEvenTypeIds = regs.GroupBy(o => new {o.Options.ClientType,
                o.Options.ResourceId, EventTypeId = o.PayloadTypeId}).Where(o => o.Count() > 1).ToArray();
            if (duplicateEvenTypeIds.Any())
            {
                throw new DuplicateEvenTypeIdDeclarationException(duplicateEvenTypeIds.SelectMany(o => o).ToArray());
            }

            foreach (var registration in regs)
            {
                _registry.Register(registration);
            }
        }

        private void BuildReceivers()
        {
            // filtering out receivers that have no message handlers setup
            var receivers = _options.Value.Receivers.Where(o => o.MessageHandlerType != null).ToArray();

            var duplicateReceivers = receivers.Where(o => o.StrictMode)
                .GroupBy(o => new { o.ResourceId, o.ClientType })
                .Where(o => o.Count() > 1).ToArray();
            // making sure there's not any duplicates in strict mode
            if (duplicateReceivers.Any())
            {
                throw new DuplicateReceiverRegistrationException(duplicateReceivers
                    .Select(o => $"{o.Key.ClientType}|{o.Key.ResourceId}").ToArray());
            }

            var receiversByName = receivers.GroupBy(o => new { o.ResourceId, o.ClientType }).ToArray();
            foreach (var groupByName in receiversByName)
            {
                // we group by connection to have one SenderWrapper by connection
                // we order by StrictMode so their name will be resolved first and avoid exceptions
                foreach (var groupByConnection in groupByName.GroupBy(o => o.ConnectionSettings)
                    .OrderByDescending(o => o.Any(opts => opts.StrictMode)))
                {
                    RegisterReceiver(groupByConnection.ToArray());
                }
            }
        }

        private void RegisterReceiver(ReceiverOptions[] receivers)
        {
            var resourceId = receivers.First().ResourceId;
            var clientType = receivers.First().ClientType;
            if (_registry.IsReceiverResourceIdTaken(clientType, resourceId))
            {
                resourceId = GetNewReceiverResourceId(clientType, resourceId);
                foreach (var sender in receivers)
                {
                    sender.UpdateResourceId(resourceId);
                }
            }

            var receiverWrapper = new ReceiverWrapper(receivers.Cast<IMessageReceiverOptions>().ToArray(),
                _options.Value, _provider);
            receiverWrapper.Initialize();
            _registry.Register(receiverWrapper);
        }

        private string GetNewReceiverResourceId(ClientType clientType, string resourceId)
        {
            var newResourceId = resourceId;
            var suffix = 2;
            while (_registry.IsReceiverResourceIdTaken(clientType, newResourceId))
            {
                newResourceId = $"{resourceId}_{suffix}";
                ++suffix;
            }

            return newResourceId;
        }

        private void BuildSenders()
        {
            var senders = _options.Value.Senders;
            var duplicateSenders = senders.Where(o => o.StrictMode)
                .GroupBy(o => new { o.ResourceId, o.ClientType })
                .Where(o => o.Count() > 1).ToArray();
            // making sure there's not any duplicates in strict mode
            if (duplicateSenders.Any())
            {
                throw new DuplicateSenderRegistrationException(duplicateSenders
                    .Select(o => $"{o.Key.ClientType}|{o.Key.ResourceId}").ToArray());
            }

            var sendersByName = senders.GroupBy(o => new { o.ResourceId, o.ClientType }).ToArray();
            foreach (var groupByName in sendersByName)
            {
                // we group by connection to have one SenderWrapper by connection
                // we order by StrictMode so their name will be resolved first and avoid exceptions
                foreach (var groupByConnection in groupByName.GroupBy(o => o.ConnectionSettings)
                    .OrderByDescending(o => o.Any(opts => opts.StrictMode)))
                {
                    RegisterSender(groupByConnection.ToArray());
                }
            }
        }

        private void RegisterSender(ClientOptions[] senders)
        {
            var resourceId = senders.First().ResourceId;
            var clientType = senders.First().ClientType;
            if (_registry.IsSenderResourceIdTaken(clientType, resourceId))
            {
                resourceId = GetNewSenderResourceId(clientType, resourceId);
                foreach (var sender in senders)
                {
                    sender.UpdateResourceId(resourceId);
                }
            }

            var senderWrapper = new SenderWrapper(senders.Cast<IClientOptions>().ToArray(), _options.Value, _provider);
            senderWrapper.Initialize();
            _registry.Register(senderWrapper);
        }

        private string GetNewSenderResourceId(ClientType clientType, string resourceId)
        {
            var newResourceId = resourceId;
            var suffix = 2;
            while (_registry.IsSenderResourceIdTaken(clientType, newResourceId))
            {
                newResourceId = $"{resourceId}_{suffix}";
                ++suffix;
            }

            return newResourceId;
        }

        public async Task StopAll()
        {
            _logger.LogInformation("[Ev.ServiceBus] Stopping azure service bus clients");

            await Task.WhenAll(_registry.GetAllSenders().Select(CloseSenderAsync).ToArray()).ConfigureAwait(false);
            await Task.WhenAll(_registry.GetAllReceivers().Select(CloseReceiverAsync).ToArray()).ConfigureAwait(false);
        }

        private async Task CloseSenderAsync(SenderWrapper sender)
        {
            if (sender.SenderClient == null)
            {
                return;
            }

            if (sender.SenderClient.IsClosedOrClosing)
            {
                return;
            }

            try
            {
                await sender.SenderClient.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Ev.ServiceBus] Closing of client {sender.ResourceId} failed");
            }
        }

        private async Task CloseReceiverAsync(ReceiverWrapper receiver)
        {
            if (receiver.ReceiverClient == null)
            {
                return;
            }

            if (receiver.ReceiverClient.IsClosedOrClosing)
            {
                return;
            }

            try
            {
                await receiver.ReceiverClient.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Ev.ServiceBus] Closing of client {receiver.ResourceId} failed");
            }
        }
    }
}
