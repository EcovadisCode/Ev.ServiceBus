using System;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.Sample.Contracts;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.Samples.Receiver.ServiceBus;

public class SecondaryWeatherEventHandler : IMessageReceptionHandler<WeatherForecast>
{
    private readonly ILogger<SecondaryWeatherEventHandler> _logger;
    private static readonly Random Random = new();
    public SecondaryWeatherEventHandler(ILogger<SecondaryWeatherEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(WeatherForecast weather, CancellationToken cancellationToken)
    {
        if(Random.Next(1, 2) == 1)
        {
            // Simulate a failure
            throw new InvalidOperationException("Simulated exception in secondary handler");
        }
        _logger.LogInformation($"Received from 2nd subscription : {weather.Date}: {weather.Summary}");

        return Task.CompletedTask;
    }
}