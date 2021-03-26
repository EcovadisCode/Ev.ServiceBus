# Ev.ServiceBus.Mvc

This NuGet helps you integrate Ev.ServiceBus with the MVC part of Asp.net core.

## Initialization

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // initialize Ev.ServiceBus
        
        // This call integrates Ev.ServiceBus with Mvc components
        services.AddMvc().AddServiceBusMvcIntegration();
    }
}
```

## Features

### Automatic dispatch

This feature completes the publish/dispatch pattern by dispatching the outgoing messages after a request is successful.
So, any published message will be automatically sent. 

With the NuGet installed : 
```csharp
[ApiController]
[Route("[controller]/[action]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IMessagePublisher _publisher;

    public WeatherForecastController(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    public void PushWeather(int count = 5)
    {
        var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast())
        .ToArray();

        // Messages here are not sent right away
        _publisher.Publish(forecasts);

        foreach (var forecast in forecasts)
        {
            _publisher.Publish(forecast);
        }

        // Messages are automatically sent when the request is successful using Ev.ServiceBus.Mvc integration.
    }
}
```

Without the NuGet installed :
```csharp
[ApiController]
[Route("[controller]/[action]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IMessageDispatcher _dispatcher;

    private readonly IMessagePublisher _publisher;

    public Alternative1Controller(IMessagePublisher publisher,
        IMessageDispatcher dispatcher)
    {
        _publisher = publisher;
        _dispatcher = dispatcher;
    }

    public void PushWeather(int count = 5)
    {
        var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast())
        .ToArray();

        // Messages here are not sent right away
        _publisher.Publish(forecasts);

        foreach (var forecast in forecasts)
        {
            _publisher.Publish(forecast);
        }

            // Messages are sent in batch when you call _dispatcher.ExecuteDispatches()
            await _dispatcher.ExecuteDispatches();
    }
}
```
