namespace Ev.ServiceBus.IntegrationEvents.Publication
{
    public interface IIntegrationEventPublisher
    {
        void Publish<TIntegrationEvent>(TIntegrationEvent integrationEvent);
    }
}
