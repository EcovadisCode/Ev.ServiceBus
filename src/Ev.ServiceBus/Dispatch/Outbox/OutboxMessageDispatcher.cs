using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Dispatch.Outbox;

public class OutboxMessageDispatcher : IMessageDispatcher
{
    private readonly IOutboxService _service;

    public OutboxMessageDispatcher(IOutboxService service)
    {
        _service = service;
    }

    public async Task ExecuteDispatches(CancellationToken token)
    {
        await _service.EagerlySendStoredMessages(token);
    }
}
