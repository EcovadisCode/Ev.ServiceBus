using System;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.Samples.AspNetCoreWeb.ServiceBus
{
    public class WeatherEventHandler : IMessageHandler
    {
        private readonly ILogger<WeatherEventHandler> _logger;

        public WeatherEventHandler(ILogger<WeatherEventHandler> logger)
        {
            _logger = logger;
        }

        public Task HandleMessageAsync(MessageContext context)
        {
            var message = context.Message;

            var weather = MessageParser.DeserializeMessage<WeatherForecast>(message);

            _logger.LogInformation($"Received from 1st subscription : {weather.Date}: {weather.Summary}");

            return Task.CompletedTask;
        }
    }
}
