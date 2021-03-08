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
    public class Alternative2Controller : ControllerBase
    {
        private readonly IDispatchSender _sender;

        private static readonly string[] Summaries =
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public Alternative2Controller(IDispatchSender sender)
        {
            _sender = sender;
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

            // Messages here are sent right away
            await _sender.SendEvents(new []{forecasts});

            foreach (var forecast in forecasts)
            {
                await _sender.SendEvents(new []{forecast});
            }
        }
    }
}
