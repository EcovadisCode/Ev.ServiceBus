using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions.Listeners;
using Ev.ServiceBus.Abstractions.MessageReception;

namespace Ev.ServiceBus.Reception;

/// <summary>
/// Default Transaction uses Diagnostics for OpenTelemetry
/// </summary>
public class DefaultTransactionManager : ITransactionManager
{
    private static readonly ActivitySource ActivitySource = new("Ev.ServiceBus");
    public async Task RunWithInTransaction(MessageExecutionContext executionContext, Func<Task> transaction)
    {
        using Activity? activity = ActivitySource.StartActivity(
            executionContext.ExecutionName,
            ActivityKind.Consumer,
            executionContext.DiagnosticId,
            new []
            {
                new KeyValuePair<string, object?>(nameof(executionContext.ClientType), executionContext.ClientType),
                new KeyValuePair<string, object?>(nameof(executionContext.ResourceId), executionContext.ResourceId),
                new KeyValuePair<string, object?>(nameof(executionContext.PayloadTypeId), executionContext.PayloadTypeId),
                new KeyValuePair<string, object?>(nameof(executionContext.HandlerName), executionContext.HandlerName),
                new KeyValuePair<string, object?>(nameof(executionContext.SessionId), executionContext.SessionId),
                new KeyValuePair<string, object?>(nameof(executionContext.MessageId), executionContext.MessageId)
            }
        );
        await transaction();
    }
}