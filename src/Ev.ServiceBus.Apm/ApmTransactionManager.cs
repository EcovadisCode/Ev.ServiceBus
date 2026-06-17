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
    // Static: Agent.AddFilter is process-wide; all ApmTransactionManager instances (one per consumer)
    // must share one filter registration and one cancelled-transaction set.

    // Tracks transaction IDs for which the ASB ProcessErrorAsync callback fired an OperationCanceledException
    // (the standard signal that the receive loop is being stopped, most commonly during pod graceful shutdown).
    // The error filter below suppresses APM error events for these transactions so that
    // shutdown-induced TaskCanceledException entries do not appear in APM.
    //
    // Cap behaviour: entries beyond CancelledTransactionIdCap are not tracked, so their error events
    // pass through the filter. This is an accepted tradeoff — reaching 1000 entries requires ~20
    // consecutive graceful processor stop/start cycles on the same pod instance, which does not occur
    // in normal Kubernetes rolling-deploy scenarios where the pod is replaced after each shutdown.
    private static readonly ConcurrentDictionary<string, byte> _cancelledTransactionIds = new();
    private const int CancelledTransactionIdCap = 1000;
    private static int _filterRegistered; // 0 = not registered, 1 = registered

    public ApmTransactionManager()
    {
        // Register the shutdown-cancellation error filter at construction time (application startup),
        // not lazily on first OnReceiveCancelled(). During pod graceful shutdown the APM agent flushes
        // its buffer concurrently with Service Bus processor teardown — registering the filter after the
        // first OperationCanceledException fires loses that race and lets error events escape to APM.
        RegisterShutdownErrorFilter();
    }

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
        if (tx is null) return;
        tx.Outcome = Outcome.Success;
        if (_cancelledTransactionIds.Count < CancelledTransactionIdCap)
            _cancelledTransactionIds.TryAdd(tx.Id, 0);

        // Fallback: if the agent was not yet configured when the constructor ran, register now.
        RegisterShutdownErrorFilter();
    }

    private static void RegisterShutdownErrorFilter()
    {
        if (!Agent.IsConfigured || Interlocked.CompareExchange(ref _filterRegistered, 1, 0) != 0)
            return;

        // Returning null from the filter drops the error event before it reaches the APM server.
        Agent.AddFilter((IError error) =>
        {
            // Case 1: transaction explicitly tracked via OnReceiveCancelled().
            if (error.TransactionId is not null && _cancelledTransactionIds.ContainsKey(error.TransactionId))
                return null;

            // Case 2: Elastic APM auto-instrumented "AzureServiceBus RECEIVE" transactions.
            // The Azure SDK ends its Activity (and therefore the APM transaction) before firing
            // ProcessErrorAsync, so Agent.Tracer.CurrentTransaction is null by the time
            // OnReceiveCancelled() runs — the transaction ID is never added to
            // _cancelledTransactionIds. Identify these by culprit pattern instead.
            // After switching to WebSockets transport, TaskCanceledException originating in
            // AmqpReceiver.ReceiveMessagesAsyncInternal only occurs during pod graceful shutdown.
            if (error.Exception?.Type is "System.Threading.Tasks.TaskCanceledException"
                                       or "System.OperationCanceledException" &&
                error.Culprit?.Contains("AmqpReceiver", StringComparison.Ordinal) == true)
                return null;

            return error;
        });
    }

    private static bool IsTraceEnabled()
        => Agent.IsConfigured && Agent.Config.Enabled && Agent.Tracer is not null && Agent.Tracer.CurrentTransaction is not null;
}