using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Dispatch.Outbox;

public class OutboxMessageDispatcher : IMessageDispatcher
{
    public Task ExecuteDispatches(CancellationToken token)
    {
        return Task.CompletedTask;
    }
}
