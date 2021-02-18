using System.Threading.Tasks;

namespace Ev.ServiceBus.Abstractions
{
    public interface IMessageDispatcher
    {
        Task DispatchEvents();
    }
}
