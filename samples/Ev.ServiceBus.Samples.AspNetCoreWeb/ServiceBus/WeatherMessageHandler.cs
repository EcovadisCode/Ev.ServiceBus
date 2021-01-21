using System;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.Samples.AspNetCoreWeb.ServiceBus
{
    public class WeatherMessageHandler : IMessageHandler
    {
        private readonly ILogger<WeatherMessageHandler> _logger;

        public WeatherMessageHandler(ILogger<WeatherMessageHandler> logger)
        {
            _logger = logger;
        }

        public Task HandleMessageAsync(MessageContext context)
        {
            var message = context.Message;

            var results = MessageParser.DeserializeMessage<WeatherForecast[]>(message);

            if (results.Length == 0)
            {
                throw new ArgumentException("Forecast should not be empty!");
            }

            foreach (var weather in results)
            {
                _logger.LogInformation($"Received from queue : {weather.Date}: {weather.Summary}");
            }

            return Task.CompletedTask;
        }
    }
}
