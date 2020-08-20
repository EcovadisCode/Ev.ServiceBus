using System;
using System.Linq;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Examples.AspNetCoreWeb.ServiceBus;
using Microsoft.AspNetCore.Mvc;

namespace Ev.ServiceBus.Examples.AspNetCoreWeb.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly IServiceBusRegistry _serviceBusRegistry;

        public WeatherForecastController(IServiceBusRegistry serviceBusRegistry)
        {
            _serviceBusRegistry = serviceBusRegistry;
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

            var queue = _serviceBusRegistry.GetQueueSender(QueuesNames.MyQueue);
            var mf = new MessageFactory();

            await queue.SendAsync(mf.Serialize(forecasts));
        }
    }
}
