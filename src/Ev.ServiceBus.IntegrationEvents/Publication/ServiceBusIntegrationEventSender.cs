using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public class ServiceBusIntegrationEventSender : IIntegrationEventSender
    {
        private readonly IServiceBusRegistry _registry;
        private readonly IMessageBodyParser _messageBodyParser;

        private const int maxMessagePerSend = 100;

        public ServiceBusIntegrationEventSender(
            IServiceBusRegistry registry,
            IMessageBodyParser messageBodyParser)
        {
            _registry = registry;
            _messageBodyParser = messageBodyParser;
        }

        public async Task SendEvents(IReadOnlyList<KeyValuePair<EventPublicationRegistration, object>> events)
        {
            foreach (var group in events.GroupBy(o => new {((ServiceBusEventPublicationRegistration)o.Key).ClientType, ((ServiceBusEventPublicationRegistration)o.Key).SenderName}))
            {
                IMessageSender sender;
                if (group.Key.ClientType == ClientType.Queue)
                {
                    sender = _registry.GetQueueSender(group.Key.SenderName);
                }
                else
                {
                    sender = _registry.GetTopicSender(group.Key.SenderName);
                }

                var messages = group.Select((o) =>
                {
                    var result = _messageBodyParser.SerializeBody(o.Value);
                    var message = MessageHelper.CreateMessage(result.ContentType, result.Body, o.Key.EventTypeId);
                    foreach (var customizer in ((ServiceBusEventPublicationRegistration) o.Key)
                        .OutgoingMessageCustomizers)
                    {
                        customizer?.Invoke(message, o.Value);
                    }

                    return message;
                }).ToList();

                var paginatedMessages = messages
                  .Select((x, i) => new { Item = x, Index = i })
                  .GroupBy(x => x.Index / maxMessagePerSend, x => x.Item);

                foreach(var pageMessages in paginatedMessages)
                {
                    await sender.SendAsync(pageMessages.Select(m => m).ToArray()).ConfigureAwait(false);
                }
            }
        }
    }
}