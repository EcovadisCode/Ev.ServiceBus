using System.Threading;
using System.Threading.Tasks;

namespace Ev.ServiceBus.Reception
{
    public interface IMessageReceptionHandler<in TMessagePayload>
    {
        Task Handle(TMessagePayload @event, CancellationToken cancellationToken);
    }
}
