using System.Threading;
using System.Threading.Tasks;

namespace Ev.ServiceBus.Abstractions;

public interface IMessageDispatcher
{
    /// <summary>
    /// Sends all the object that have been stored temporarily with <see cref="IMessagePublisher.Publish{TMessagePayload}"/>.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    Task ExecuteDispatches(CancellationToken token);
}