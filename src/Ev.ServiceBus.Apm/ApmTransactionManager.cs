using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Ev.ServiceBus.Abstractions.Listeners;
using Ev.ServiceBus.Abstractions.MessageReception;

namespace Ev.ServiceBus.Apm;

/// <summary>
/// Default Transaction uses Diagnostics for Elastic APM
/// </summary>
public class ApmTransactionManager : ITransactionManager, ICancellationAwareTransactionManager
{
    // Tracks transaction IDs whose receive loop was cancelled during pod shutdown.
    // The error filter below suppresses APM error events for these transactions so that
    // shutdown-induced TaskCanceledException entries do not appear in APM.
    // The set is bounded: each pod restart adds ~50 entries at most; the pod is replaced shortly after.
    private static readonly ConcurrentDictionary<string, byte> _cancelledTransactionIds = new();
    private static int _filterRegistered; // 0 = not registered, 1 = registered
    public async Task RunWithInTransaction(MessageExecutionContext executionContext, Func<Task> transaction)
    {
        if (IsTraceEnabled())
        {
            Agent.Tracer.CurrentTransaction.Name = executionContext.ExecutionName;
            Agent.Tracer.CurrentTransaction.SetLabel(
                nameof(executionContext.ClientType),
                executionContext.ClientType);
            Agent.Tracer.CurrentTransaction.SetLabel(
                nameof(executionContext.ResourceId),
                executionContext.ResourceId);
            Agent.Tracer.CurrentTransaction.SetLabel(
                nameof(executionContext.PayloadTypeId),
                executionContext.PayloadTypeId);
            Agent.Tracer.CurrentTransaction.SetLabel(
                nameof(executionContext.HandlerName),
                executionContext.HandlerName);
            Agent.Tracer.CurrentTransaction.SetLabel(
                nameof(executionContext.SessionId),
                executionContext.SessionId);
            Agent.Tracer.CurrentTransaction.SetLabel(
                nameof(executionContext.MessageId),
                executionContext.MessageId);


            var spanLinks = GetSpanLinks(executionContext.DiagnosticId);
            await Agent.Tracer.CurrentTransaction.CaptureSpan(
                $"{Agent.Tracer.CurrentTransaction.Name} PROCESS",
                ApiConstants.TypeMessaging,
                async () =>
                {
                    await transaction();
                },
                links: spanLinks
            );
        }
        else
        {
            await transaction();
        }
    }

    private static List<SpanLink> GetSpanLinks(string? diagnosticId)
    {
        var parentContext = ActivityContext.TryParse(diagnosticId, null, out var parentContextParsed)
            ? parentContextParsed
            : default;
        var spanLinks = new List<SpanLink>();

        if (parentContext != default)
        {
            spanLinks.Add(new SpanLink(parentContext.SpanId.ToString(), parentContext.TraceId.ToString()));
        }

        return spanLinks;
    }

    public void OnReceiveCancelled()
    {
        if (!IsTraceEnabled())
            return;

        var tx = Agent.Tracer.CurrentTransaction;
        tx.Outcome = Outcome.Success;
        _cancelledTransactionIds.TryAdd(tx.Id, 0);

        // Register once: suppress error events for cancelled-receive transactions before they are sent to APM.
        // Elastic APM captures error events at the DiagnosticSource level (before ReceiverWrapper runs),
        // so setting Outcome = Success alone does not prevent error documents from appearing in APM.
        // Returning null from the filter drops the error event entirely.
        if (Interlocked.CompareExchange(ref _filterRegistered, 1, 0) == 0)
        {
            Agent.AddFilter((IError error) =>
                error.TransactionId is not null && _cancelledTransactionIds.ContainsKey(error.TransactionId)
                    ? null
                    : error);
        }
    }

    private static bool IsTraceEnabled()
        => Agent.IsConfigured && Agent.Config.Enabled && Agent.Tracer is not null && Agent.Tracer.CurrentTransaction is not null;
}