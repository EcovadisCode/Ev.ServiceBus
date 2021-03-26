using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public interface IExceptionHandler
    {
        /// <summary>
        /// This is called whenever there's an unhandled exception during the reception process.
        /// </summary>
        /// <param name="exceptionReceivedEventArgs"></param>
        /// <returns></returns>
        Task HandleExceptionAsync(ExceptionReceivedEventArgs exceptionReceivedEventArgs);
    }
}
