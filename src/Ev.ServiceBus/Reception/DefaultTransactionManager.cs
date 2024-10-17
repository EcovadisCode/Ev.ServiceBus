using System;
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
    public async Task RunWithInTransaction(MessageExecutionContext executionContext, Func<Task> transaction)
    {
        if (Activity.Current != null)
        {
            Activity.Current.DisplayName = executionContext.ExecutionName;
            Activity.Current.SetTag(nameof(executionContext.ClientType), executionContext.ClientType);
            Activity.Current.SetTag(nameof(executionContext.ResourceId), executionContext.ResourceId);
            Activity.Current.SetTag(nameof(executionContext.PayloadTypeId), executionContext.PayloadTypeId);
            Activity.Current.SetTag(nameof(executionContext.HandlerName), executionContext.HandlerName);
            Activity.Current.SetTag(nameof(executionContext.SessionId), executionContext.SessionId);
            Activity.Current.SetTag(nameof(executionContext.MessageId), executionContext.MessageId);
        }

        await transaction();
    }
}