using Elastic.Apm;
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
        }

        await transaction();
    }

    private static bool IsTraceEnabled()
        => Agent.IsConfigured && Agent.Config.Enabled && Agent.Tracer is not null;
}