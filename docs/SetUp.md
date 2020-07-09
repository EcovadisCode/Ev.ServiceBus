# Set-Up

## .Net Core Web Project
All you need to do to be sure that everything works as expected is the following code:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceBus(/* enabled: true, receiveMessages: true */);
}
```

Thanks to that, the necessary services of this service-bus library are working in the background.
To learn more about this mechanism [read here](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/background-tasks-with-ihostedservice)
on official microsoft pages.

You can specify whether interactions with a servicebus will be `enabled` (by default `true`)
or whether you want only to send messages (`receiveMessages = true` by default). 

## Other types of projects

To all work as expected, you have to ensure that ```ServiceBusHost``` is started and properly stopped.

```csharp
services.AddServiceBus();
var serviceProvider = services.BuildServiceProvider();

var hostedServices = serviceProvider.GetServices<IHostedService>();
var host = hostedServices.First(o => o is ServiceBusHost);
await host.StartAll();
/*
Code here...
*/
await host.StopAll();
```
