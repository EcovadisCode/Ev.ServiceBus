using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ev.ServiceBus.Abstractions.Batching;

public interface IMessageBatcher
{
    Task<IReadOnlyCollection<MessageBatch<T>>> CalculateBatches<T>(IEnumerable<T> payloads);
}