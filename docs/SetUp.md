# Set-Up

## .Net Core Web Project
You will need to do two things to setup the NuGet :
1. Implement a class inheriting from `IMessagePayloadSerializer`.
2. Call `service.AddServiceBus<>()` method.

```csharp
public class PayloadSerializer : IMessagePayloadSerializer
{
    public SerializationResult SerializeBody(object objectToSerialize)
    {
        // ...
    }

    public object DeSerializeBody(byte[] content, Type typeToCreate)
    {
        // ...
    }
}

public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceBus(settings => {
        settings.Enabled = true;
        settings.ReceiveMessages = true;
        settings.WithConnection("", new ServiceBusClientOptions());
    });
}
```

> Concretely, we don't know how you (the user) want to serialize your service bus messages.
> So we decided to give you full control over that.

Once this is done, the necessary services of this service-bus library are working in the background.
To learn more about this mechanism [read here](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/background-tasks-with-ihostedservice)
on official microsoft pages.

You can deactivate the reception and sending of messages with the `Enabled` setting. The methods sending messages will not throw exceptions while in this state.
Alternatively, you can deactivate only the reception of messages using the `ReceiveMessages` setting.

Calling `settings.WithConnection()` will setup a default connection that all your registrations will use.

## Other types of projects

For everything to work as expected, you have to ensure that `ServiceBusHost` is started and properly stopped.

```csharp
services.AddServiceBus(settings => {});
var serviceProvider = services.BuildServiceProvider();

var hostedServices = serviceProvider.GetServices<IHostedService>();
var host = hostedServices.First(o => o is ServiceBusHost);
await host.StartAsync();
/*
Code here...
*/
await host.StopAsync();
```
