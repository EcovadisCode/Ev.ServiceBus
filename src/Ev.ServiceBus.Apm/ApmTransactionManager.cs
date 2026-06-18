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
    // Note for tests: static state is shared across test runs within the same process.
    // Call ResetForTests() in test setup/teardown to isolate tests from each other.

    // Tracks transaction IDs for which the ASB ProcessErrorAsync callback fired an OperationCanceledException
    // (the standard signal that the receive loop is being stopped, most commonly during pod graceful shutdown).
    // The error filter below suppresses APM error events for these transactions so that
    // shutdown-induced TaskCanceledException entries do not appear in APM.
    //
    // IDs are removed from this set after the filter matches them (TryRemove) to keep the dictionary lean.
    // Cap behaviour: entries beyond CancelledTransactionIdCap are not tracked, so their error events
    // fall through to the culprit-based filter path (Case 2 in ShouldSuppressError). This is an accepted
    // tradeoff — reaching 1000 entries requires ~20 consecutive graceful processor stop/start cycles on
    // the same pod instance, which does not occur in normal Kubernetes rolling-deploy scenarios.
    // If the cap were reached by non-AmqpReceiver OperationCanceledException paths (i.e. user-code
    // cancellations unrelated to AMQP shutdown), those excess error events would not be suppressed by
    // either Case 1 or Case 2 and would reach the APM server. This is acceptable: the conditions
    // required to reach the cap via that path are not reachable in practice.
    private static readonly ConcurrentDictionary<string, byte> _cancelledTransactionIds = new();
    private const int CancelledTransactionIdCap = 1000;
    private static int _filterRegistered; // 0 = not registered, 1 = registered (Interlocked.CompareExchange requires int)

    // Azure.Messaging.ServiceBus internal class name that appears in the APM error culprit when
    // the AMQP receive loop is cancelled. After switching to WebSockets transport (PR #202138),
    // this culprit only fires during pod graceful shutdown — not from network drops (which surface
    // as ServiceBusException, not TaskCanceledException).
    private const string AmqpReceiverCulprit = "AmqpReceiver";

    public ApmTransactionManager()
    {
        // Best-effort registration at construction time. If the Elastic APM hosted service starts
        // AFTER the Service Bus hosted service (the typical registration order), Agent.IsConfigured
        // is still false here and registration is deferred to the first RunWithInTransaction call.
        RegisterShutdownErrorFilter();
    }

    public async Task RunWithInTransaction(MessageExecutionContext executionContext, Func<Task> transaction)
    {
        if (IsTraceEnabled())
        {
            // Ensure the filter is registered before any message processing completes.
            // This is the reliable registration point: by the time IsTraceEnabled() returns true,
            // Agent.IsConfigured is guaranteed true — covering the case where the APM hosted service
            // started after the Service Bus hosted service and the constructor registration was skipped.
            RegisterShutdownErrorFilter();
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
        // Attempt registration before checking IsTraceEnabled — the filter must be in place even
        // for auto-instrumented "AzureServiceBus RECEIVE" transactions where CurrentTransaction is
        // null (Case 2). After this point it is too late for the current error batch, but
        // registering here ensures coverage if RunWithInTransaction was never reached.
        RegisterShutdownErrorFilter();

        if (!IsTraceEnabled())
            return;

        var tx = Agent.Tracer.CurrentTransaction;
        if (tx is null) return;
        tx.Outcome = Outcome.Success;
        // Count check and TryAdd are not atomic — under high concurrency the dictionary may reach
        // CancelledTransactionIdCap + N entries. The cap is a soft limit; this is intentional.
        if (_cancelledTransactionIds.Count < CancelledTransactionIdCap)
            _cancelledTransactionIds.TryAdd(tx.Id, 0);
        // If Count >= CancelledTransactionIdCap, this ID is not tracked here.
        // The culprit-based path (Case 2 in ShouldSuppressError) still suppresses the error.
    }

    private static void RegisterShutdownErrorFilter()
    {
        if (!Agent.IsConfigured || Interlocked.CompareExchange(ref _filterRegistered, 1, 0) != 0)
            return;

        // Returning null from the filter drops the error event before it reaches the APM server.
        Agent.AddFilter((IError error) =>
            ShouldSuppressError(error.TransactionId, error.Exception?.Type, error.Culprit)
                ? null
                : error);
    }

    // Extracted for unit testability. Called by the APM filter lambda.
    internal static bool ShouldSuppressError(string? transactionId, string? exceptionType, string? culprit)
    {
        // Case 1: transaction explicitly tracked via OnReceiveCancelled().
        // TryRemove keeps the dictionary lean — matched IDs are consumed on first use.
        // No exceptionType guard is applied here: _cancelledTransactionIds is populated exclusively
        // by OnReceiveCancelled(), which ReceiverWrapper only calls for OperationCanceledException.
        // Any ID present in this set therefore already originates from a cancellation path.
        if (transactionId is not null && _cancelledTransactionIds.TryRemove(transactionId, out _))
            return true;

        // Case 2: Elastic APM auto-instrumented "AzureServiceBus RECEIVE" transactions.
        // The Azure SDK ends its Activity (and therefore the APM transaction) before firing
        // ProcessErrorAsync, so Agent.Tracer.CurrentTransaction is null when OnReceiveCancelled()
        // runs — the transaction ID is never added to _cancelledTransactionIds.
        // After switching to WebSockets transport, TaskCanceledException originating in
        // AmqpReceiver.ReceiveMessagesAsyncInternal only occurs during pod graceful shutdown.
        return (exceptionType is "System.Threading.Tasks.TaskCanceledException"
                               or "System.OperationCanceledException") &&
               culprit?.Contains(AmqpReceiverCulprit, StringComparison.Ordinal) == true;
    }

    // For test isolation only — resets static state between test runs in the same process.
    internal static void ResetForTests()
    {
        _cancelledTransactionIds.Clear();
        Interlocked.Exchange(ref _filterRegistered, 0);
    }

    // For test setup only — seeds a transaction ID as if OnReceiveCancelled() had recorded it.
    internal static void AddCancelledTransactionIdForTests(string id) =>
        _cancelledTransactionIds.TryAdd(id, 0);

    private static bool IsTraceEnabled()
        => Agent.IsConfigured && Agent.Config.Enabled && Agent.Tracer is not null && Agent.Tracer.CurrentTransaction is not null;
}
