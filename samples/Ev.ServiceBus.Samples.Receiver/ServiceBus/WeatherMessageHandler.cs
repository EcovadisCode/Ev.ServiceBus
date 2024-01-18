using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.Sample.Contracts;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.Samples.Receiver.ServiceBus;

public class WeatherMessageHandler : IMessageReceptionHandler<WeatherForecast[]>
{
    private readonly ILogger<WeatherMessageHandler> _logger;

    public WeatherMessageHandler(ILogger<WeatherMessageHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(WeatherForecast[] results, CancellationToken cancellationToken)
    {
        foreach (var weather in results)
        {
            _logger.LogInformation($"Received from queue : {weather.Date}: {weather.Summary}");
        }

        return Task.CompletedTask;
    }
}