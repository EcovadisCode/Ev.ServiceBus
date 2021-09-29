# Advanced scenarios

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
    services.AddServiceBus<PayloadSerializer>(settings => {
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

## Listening to internal events

You can register a listener class that will be called every time the execution of a message starts, is successful and/or has failed. 

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceBus<PayloadSerializer>(settings => {
        settings.WithConnection(serviceBusConnectionString);
    })
    .RegisterEventListener<ServiceBusEventListener>();
}

public class ServiceBusEventListener : IServiceBusEventListener
{
    public Task OnExecutionStart(ExecutionStartedArgs args)
    {
        return Task.CompletedTask;
    }

    public Task OnExecutionSuccess(ExecutionSucceededArgs args)
    {
        return Task.CompletedTask;
    }

    public Task OnExecutionFailed(ExecutionFailedArgs args)
    {
        return Task.CompletedTask;
    }
}
```

## Async Api schema generation with Saunter

With `Ev.ServiceBus.AsyncApi` package, you can populate your AsyncApi schema 
with resources and models registered by `Ev.ServiceBus`. 
You can visit [Saunter](https://github.com/tehmantra/saunter) or [asyncapi.com](https://www.asyncapi.com/) 
for more information about Async Api specifications.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddAsyncApiSchemaGeneration(options => {
        options.AsyncApi = new AsyncApiDocument()
        {
            Info = new Info("Receiver API", "1.0.0")
            {
                Description = "Sample receiver project",
            }
        };
    });

    services.AddServiceBus<MessagePayloadSerializer>(settings => {
        settings.WithConnection(serviceBusConnectionString);
    })
    .PopulateAsyncApiSchemaWithEvServiceBus();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapAsyncApiDocuments();
        endpoints.MapAsyncApiUi();
    });
}
```