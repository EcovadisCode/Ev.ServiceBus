namespace Ev.ServiceBus.Abstractions
{
    public interface IMessagePublisher
    {
        void Publish<TMessagePayload>(TMessagePayload messageDto);
    }
}
