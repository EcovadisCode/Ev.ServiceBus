using System;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.Samples.AspNetCoreWeb.ServiceBus
{
    public class SecondaryWeatherEventHandler : IMessageHandler
    {
        private readonly ILogger<SecondaryWeatherEventHandler> _logger;

        public SecondaryWeatherEventHandler(ILogger<SecondaryWeatherEventHandler> logger)
        {
            _logger = logger;
        }

        public Task HandleMessageAsync(MessageContext context)
        {
            var message = context.Message;

            var weather = MessageParser.DeserializeMessage<WeatherForecast>(message);

            _logger.LogInformation($"Received from 2nd subscription : {weather.Date}: {weather.Summary}");

            return Task.CompletedTask;
        }
    }
}
