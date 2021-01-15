# Set-Up

## .Net Core Web Project
All you need to do to be sure that everything works as expected is the following code:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceBus(settings => {
        settings.Enabled = true;
        settings.ReceiveMessages = true;
        settings.WithConnection();
    });
}
```

Thanks to that, the necessary services of this service-bus library are working in the background.
To learn more about this mechanism [read here](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/background-tasks-with-ihostedservice)
on official microsoft pages.

You can deactivate the reception and sending of messages with the `Enabled` setting. The methods sending messages will not throw exceptions while in this state.
Alternatively, you can deactivate only the reception of messages using the `ReceiveMessages` setting.

Calling `settings.WithConnection()` will setup a default connection that all your registrations will use.

## Other types of projects

To all work as expected, you have to ensure that ```ServiceBusHost``` is started and properly stopped.

```csharp
services.AddServiceBus();
var serviceProvider = services.BuildServiceProvider();

var hostedServices = serviceProvider.GetServices<IHostedService>();
var host = hostedServices.First(o => o is ServiceBusHost);
await host.StartAsync();
/*
Code here...
*/
await host.StartAsync();
```
