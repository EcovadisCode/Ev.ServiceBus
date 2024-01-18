using System.Threading;
using System.Threading.Tasks;

namespace Ev.ServiceBus.Reception;

/// <summary>
/// Base interface for a message reception handler.
/// </summary>
/// <typeparam name="TMessagePayload">The type of object this interface will process</typeparam>
public interface IMessageReceptionHandler<in TMessagePayload>
{
    /// <summary>
    /// Called whenever a message of type <see cref="TMessagePayload"/> is received.
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Handle(TMessagePayload @event, CancellationToken cancellationToken);
}