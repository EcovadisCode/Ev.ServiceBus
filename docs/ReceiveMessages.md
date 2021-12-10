# How to receive messages

To start receiving messages from `queues` or/and `subscriptions` let's register them first.

### Configure Services

The registration process is very simple:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Initialize serviceBus

    services.RegisterServiceBusReception().FromQueue("myqueue", builder =>
    {
        builder.RegisterReception<WeatherForecast[], WeatherMessageHandler>();
    });

    services.RegisterServiceBusReception().FromSubscription("mytopic", "mysubscription", builder =>
    {
        builder.RegisterReception<WeatherForecast, WeatherEventHandler>();
    });
}
```
The above example is the minimum to register for receiving messages.
The provided names (`myqueue` and `mytopic` or `mysubscription`) must be the names of resources present on your [Azure Service Bus namespace](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview#namespaces).

> You cannot register the same reception object twice throughout the entire application.
> A message that is received will always be processed by one and only one handler.

For each reception registration, you must implement two classes :
- The deserialized payload that will be received from the resource
- The handler that will receive said deserialized payload, inheriting from `IMessageReceptionHandler`.

```csharp
[Serializable]
public class WeatherForecast
{
    public DateTime Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string Summary { get; set; } = "";
}

public class WeatherEventHandler : IMessageReceptionHandler<WeatherForecast>
{
    private readonly ILogger<WeatherEventHandler> _logger;

    public WeatherEventHandler(ILogger<WeatherEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(WeatherForecast weather, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Received from 1st subscription : {weather.Date}: {weather.Summary}");

        return Task.CompletedTask;
    }
}
```
This service is registered in the IOC container as scoped and will be resolved once for every received message.

### Advanced features

#### Customizing the PayloadTypeId

> Reminder: the PayloadTypeId is a user property on the service bus message itself. 
> During reception, it determines which object should the message be deserialized into.

By default, the PayloadTypeId is set automatically to be the name of the type that will be sent.
If you are not satisfied with that naming, you can call `.CustomizePayloadTypeId()`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Initialize serviceBus
    
    services.RegisterServiceBusReception().FromSubscription("mytopic", "mysubscription", builder =>
    {
        // The default name for this dispatch is "WeatherForecast"
        builder.RegisterReception<WeatherForecast, WeatherEventHandler>()
               .CustomizePayloadTypeId("Forecast");
    });
}
```

#### Access the correlation Id and other metadata of the message

You can use the `IMessageMetadataAccessor` interface to access the metadatas of your received message.
You just need to inject the interface into one of your service currently executing a message.

```csharp
public class WeatherEventHandler : IMessageReceptionHandler<WeatherForecast>
{
    private readonly ILogger<WeatherEventHandler> _logger;
    private readonly IMessageMetadataAccessor _metadataAccessor;

    public WeatherEventHandler(ILogger<WeatherEventHandler> logger, IMessageMetadataAccessor metadataAccessor)
    {
        _logger = logger;
        _metadataAccessor = metadataAccessor;
    }

    public Task Handle(WeatherForecast weather, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Received from 1st subscription : {weather.Date}: {weather.Summary}");
        _logger.LogInformation($"CorrelationId is : {_metadataAccessor.Metadata.CorrelationId}");

        return Task.CompletedTask;
    }
}
```