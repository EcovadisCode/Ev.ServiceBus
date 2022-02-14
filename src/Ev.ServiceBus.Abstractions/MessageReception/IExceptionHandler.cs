using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions;

public interface IExceptionHandler
{
    /// <summary>
    /// This is called whenever there's an unhandled exception during the reception process.
    /// </summary>
    /// <param name="exceptionReceivedEventArgs"></param>
    /// <returns></returns>
    Task HandleExceptionAsync(ProcessErrorEventArgs exceptionReceivedEventArgs);
}