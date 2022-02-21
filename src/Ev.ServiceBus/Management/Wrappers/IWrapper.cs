using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus;

public interface IWrapper
{
    public string ResourceId { get; }
    public ClientType ClientType { get; }
    public Task CloseAsync(CancellationToken cancellationToken);
}
