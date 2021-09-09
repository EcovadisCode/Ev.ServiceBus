using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus.Samples.Receiver
{
    public class ServiceBusEventListener : IServiceBusEventListener
    {
        public Task OnExecutionStart(ExecutionStartedArgs args)
        {
            return Task.CompletedTask;
        }

        public Task OnExecutionSuccess(ExecutionSucceededArgs args)
        {
            return Task.CompletedTask;
        }

        public Task OnExecutionFailed(ExecutionFailedArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
