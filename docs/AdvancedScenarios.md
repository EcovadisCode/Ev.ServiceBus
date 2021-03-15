﻿# Advanced scenarios

## Customizing the connection to service bus

The method `.WithConnection()` as several different signatures. With them you can create a connection using either :
- a connection string.
- a `ServiceBusConnection` object.
- a `ServiceBusConnectionStringBuilder` object.

You can also use the `.WithConnection()` method to set the 
[ReceiveMode](https://docs.microsoft.com/en-us/dotnet/api/microsoft.servicebus.messaging.receivemode?view=azure-dotnet) 
and [RetryPolicy](https://docs.microsoft.com/en-us/dotnet/api/microsoft.servicebus.retrypolicy?view=azure-dotnet) for that connection.

examples :
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceBus<PayloadSerializer>(settings => {
        settings.WithConnection(serviceBusConnectionString, ReceiveMode.ReceiveAndDelete);
        settings.WithConnection(new ServiceBusConnection(), ReceiveMode.PeekLock, new CustomRetryPolicy());
        settings.WithConnection(new ServiceBusConnectionStringBuilder());
    });
}
```

## Overriding the default connection

Generally, you only need one connection for your application to run service bus.
If, for some reason, you need to set another connection for a specific registration, 
you can override the default connection by calling `.WithConnection()` on the registration itself. 

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceBus(settings => {
        settings.WithConnection(serviceBusConnectionString);
    });

    services.RegisterServiceBusDispatch().ToQueue("myqueue", builder =>
    {
        builder.CustomizeConnection(ConnectionString2);
        builder.RegisterDispatch<WeatherForecast[]>();
    });

    services.RegisterServiceBusDispatch().ToTopic("mytopic", builder =>
    {
        builder.CustomizeConnection(ConnectionString3);
        builder.RegisterDispatch<WeatherForecast>();
    });
    
    services.RegisterServiceBusReception().FromQueue("myqueue", builder =>
    {
        builder.CustomizeConnection(ConnectionString4);
        builder.RegisterReception<WeatherForecast[], WeatherMessageHandler>();
    });

    services.RegisterServiceBusReception().FromSubscription("mytopic", "mysubscription",
        builder =>
        {
            builder.CustomizeConnection(ConnectionString5);
            builder.RegisterReception<WeatherForecast, WeatherEventHandler>();
        });
}
```
