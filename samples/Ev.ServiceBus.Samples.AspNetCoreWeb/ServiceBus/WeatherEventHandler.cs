using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Reception;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.Samples.AspNetCoreWeb.ServiceBus
{
    public class WeatherEventHandler : IMessageReceptionHandler<WeatherForecast>
    {
        private readonly ILogger<WeatherEventHandler> _logger;

        public WeatherEventHandler(ILogger<WeatherEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(WeatherForecast weather, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Received from 1st subscription : {weather.Date}: {weather.Summary}");

            return Task.CompletedTask;
        }
    }
}
