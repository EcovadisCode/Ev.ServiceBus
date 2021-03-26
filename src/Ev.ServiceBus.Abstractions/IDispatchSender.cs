using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ev.ServiceBus.Abstractions
{
    public interface IDispatchSender
    {
        /// <summary>
        /// Immediately serializes object to messages and sends them through ServiceBus.
        /// (This is used internally by <see cref="IMessageDispatcher.ExecuteDispatches"/>)
        /// </summary>
        /// <param name="messagePayloads"></param>
        /// <returns></returns>
        Task SendDispatches(IEnumerable<object> messagePayloads);
    }
}
