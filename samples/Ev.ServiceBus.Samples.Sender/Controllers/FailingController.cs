using System;
using System.Linq;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Sample.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Ev.ServiceBus.Samples.Sender.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class FailingController : ControllerBase
{
    private static readonly string[] Summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly IMessagePublisher _publisher;

    public FailingController(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    public void PushWeather(int count = 5)
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

        // Since an exception has been thrown events won't be emitted
        throw new ArgumentException();
    }
}