using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Ev.ServiceBus.Abstractions.Listeners;
using Ev.ServiceBus.Abstractions.MessageReception;

namespace Ev.ServiceBus.Apm;

/// <summary>
/// Default Transaction uses Diagnostics for Elastic APM
/// </summary>
public class ApmTransactionManager : ITransactionManager
{
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

    private static bool IsTraceEnabled()
        => Agent.IsConfigured && Agent.Config.Enabled && Agent.Tracer is not null && Agent.Tracer.CurrentTransaction is not null;
}