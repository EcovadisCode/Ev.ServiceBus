using System;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Ev.ServiceBus.Abstractions.Listeners;
using Ev.ServiceBus.Abstractions.MessageReception;

namespace Ev.ServiceBus.Apm;

public class ApmTransactionManager : ITransactionManager
{
    public async Task RunWithInTransaction(MessageExecutionContext executionContext, Func<Task> transaction)
    {
        if (IsTraceEnabled())
        {
            await Agent.Tracer.CaptureTransaction(
                executionContext.ExecutionName,
                ApiConstants.TypeMessaging,
                async () =>
                {
                    Agent.Tracer.CurrentTransaction?
                        .SetLabel(nameof(executionContext.ClientType), executionContext.ClientType);
                    Agent.Tracer.CurrentTransaction?
                        .SetLabel(nameof(executionContext.ResourceId), executionContext.ResourceId);
                    Agent.Tracer.CurrentTransaction?
                        .SetLabel(nameof(executionContext.PayloadTypeId), executionContext.PayloadTypeId);
                    Agent.Tracer.CurrentTransaction?
                        .SetLabel(nameof(executionContext.HandlerName), executionContext.HandlerName);
                    Agent.Tracer.CurrentTransaction?
                        .SetLabel(nameof(executionContext.SessionId), executionContext.SessionId);
                    Agent.Tracer.CurrentTransaction?
                        .SetLabel(nameof(executionContext.MessageId), executionContext.MessageId);
                    await transaction();
                },
                DistributedTracingData.TryDeserializeFromString(executionContext.DiagnosticId));
        }
        else
        {
            await transaction();
        }
    }

    private static bool IsTraceEnabled()
        => Agent.IsConfigured && Agent.Config.Enabled && Agent.Tracer is not null;
}