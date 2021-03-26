using System.Threading.Tasks;

namespace Ev.ServiceBus.Abstractions
{
    public interface IMessageDispatcher
    {
        /// <summary>
        /// Sends all the object that have been stored temporarily with <see cref="IMessagePublisher.Publish{TMessagePayload}"/>.
        /// </summary>
        /// <returns></returns>
        Task ExecuteDispatches();
    }
}
