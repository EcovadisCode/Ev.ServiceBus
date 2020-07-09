using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public interface IExceptionHandler
    {
        Task HandleExceptionAsync(ExceptionReceivedEventArgs exceptionReceivedEventArgs);
    }
}
