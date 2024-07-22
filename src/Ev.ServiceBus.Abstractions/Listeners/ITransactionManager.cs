using System;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions.MessageReception;

namespace Ev.ServiceBus.Abstractions.Listeners;

public interface ITransactionManager
{
    public Task RunWithInTransaction(
        MessageExecutionContext executionContext,
        Func<Task> transaction);
}