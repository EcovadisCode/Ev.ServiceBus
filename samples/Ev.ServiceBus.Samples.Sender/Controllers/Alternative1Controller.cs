using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Sample.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Ev.ServiceBus.Samples.Sender.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class Alternative1Controller : ControllerBase
{
    private static readonly string[] Summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly IMessageDispatcher _dispatcher;

    private readonly IMessagePublisher _publisher;

    public Alternative1Controller(IMessagePublisher publisher,
        IMessageDispatcher dispatcher)
    {
        _publisher = publisher;
        _dispatcher = dispatcher;
    }

    public async Task PushWeather(CancellationToken token, int count = 5)
    {
        var rng = new Random();
        var forecasts = Enumerable.Range(1, count).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();

        // Messages here are not sent right away
        _publisher.Publish(forecasts);

        foreach (var forecast in forecasts)
        {
            _publisher.Publish(forecast);
        }

        // Messages are sent in batch when you call _dispatcher.ExecuteDispatches()
        await _dispatcher.ExecuteDispatches(token);
    }
}