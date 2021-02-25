using System;
using System.Linq;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Sample.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Ev.ServiceBus.Samples.Sender.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly IMessagePublisher _publisher;
        private readonly IMessageDispatcher _dispatcher;

        public WeatherForecastController(
            IMessagePublisher publisher,
            IMessageDispatcher dispatcher)
        {
            _publisher = publisher;
            _dispatcher = dispatcher;
        }

        public async Task PushWeather(int count = 5)
        {
            var rng = new Random();
            var forecasts = Enumerable.Range(1, count).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();

            _publisher.Publish(forecasts);

            foreach (var forecast in forecasts)
            {
                _publisher.Publish(forecast);
            }

            await _dispatcher.DispatchEvents();
        }
    }
}
