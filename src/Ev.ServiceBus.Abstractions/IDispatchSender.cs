using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ev.ServiceBus.Abstractions
{
    public interface IDispatchSender
    {
        Task SendEvents(IEnumerable<object> messagePayloads);
    }
}
