using System.Threading.Tasks;

namespace Ev.ServiceBus.Abstractions;

public interface IServiceBusEventListener
{
    Task OnExecutionStart(ExecutionStartedArgs args);
    Task OnExecutionSuccess(ExecutionSucceededArgs args);
    Task OnExecutionFailed(ExecutionFailedArgs args);
}