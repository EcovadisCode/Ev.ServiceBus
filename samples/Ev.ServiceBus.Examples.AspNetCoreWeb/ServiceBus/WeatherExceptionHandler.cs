using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Ev.ServiceBus.Examples.AspNetCoreWeb
{
    public class WeatherExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<WeatherExceptionHandler> _logger;

        public WeatherExceptionHandler(ILogger<WeatherExceptionHandler> logger)
        {
            _logger = logger;
        }

        public Task HandleExceptionAsync(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            if (exceptionReceivedEventArgs.Exception is ArgumentException ae)
            {
                _logger.LogError(ae.Message);
            }
            else
            {
                _logger.LogCritical(
                    exceptionReceivedEventArgs.Exception,
                    $"Something critical happenned during: {exceptionReceivedEventArgs.ExceptionReceivedContext.Action}.");
            }
            return Task.CompletedTask;
        }
    }
}
