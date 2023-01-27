using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ev.ServiceBus.Abstractions.Batching;

public sealed class MessageBatch<T> : IEnumerable<T>
{
    private readonly IReadOnlyCollection<T> _payloads;

    internal MessageBatch(IEnumerable<T> payloads)
    {
        _payloads = payloads is not null
            ? payloads.ToList()
            : throw new ArgumentNullException(nameof(payloads));
    }

    public IEnumerator<T> GetEnumerator() => _payloads.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}