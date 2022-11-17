# How to send messages

To start sending messages to `queues` or/and `topics` let's register them first.

### Configure Services

The registration process is very simple:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Initialize serviceBus
    
    services.RegisterServiceBusDispatch().ToQueue("myqueue", builder =>
    {
        builder.RegisterDispatch<WeatherForecast[]>();
    });

    services.RegisterServiceBusDispatch().ToTopic("mytopic", builder =>
    {
        builder.RegisterDispatch<WeatherForecast>();
    });
}
```
The above example is the minimum to register for sending messages.
The provided names (`mytopic` or `myqueue`) must be the names of resources present on your [Azure Service Bus namespace](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview#namespaces).
Types registered as dispatches must be simple objects that can be easily serialized.

> You can register the same dispatch once on each different queues or topic you want. The dispatch will then generate one message for each queues/topic. 
> 
> You cannot register the same dispatch twice into a single queue/topic

### Sending messages

There are several ways you can use to send messages

#### Use the dispatch sender
> This is the simplest way to send messages, but it is not recommended to use it (explanations below)

You can inject the `IDispatchSender` service to send messages:

```csharp
var sender = _serviceProvider.GetService<IDispatchSender>();
var forecast = new WeatherForecast();
await sender.SendDispatches(new []{forecast});
```

With this service, you can send objects to service bus the moment `SendDispatches` is called.
The NuGet will figure out through which queue/topic to send this object depending on how you configured it.

But most of the time, you want to create those objects to send in the middle of your business code, which is transactional.
So, if anything goes wrong after you've sent the messages, everything will be rolled back except your messages.

You can also send scheduled dispatches. The messages sent that way will be stored on the queue/topic 
and be sent to the receivers at a specific datetime of your choosing.
```csharp
var sender = _serviceProvider.GetService<IDispatchSender>();
var forecast = new WeatherForecast();
await sender.ScheduleDispatches(new []{forecast}, DateTimeOffset.UtcNow.AddDays(1));
```

#### Use the publish/dispatch pattern

To counter the problem explained above, you can use two more services: 
```csharp
// transaction begin
// business code...

var publisher = _serviceProvider.GetService<IMessagePublisher>();
var forecast = new WeatherForecast();
publisher.Publish(forecast);

// business code...
// transaction end

var dispatcher = _serviceProvider.GetService<IMessageDispatcher>();
var forecast = new WeatherForecast();
await publisher.ExecuteDispatches(forecast);
```
Using the `IMessagePublisher` service, you can temporarily store objects to send.

And, at the moment of your choosing, you can use the `IMessageDispatcher` service to send all stored messages at once.

> There's another NuGet that you can use called 'Ev.ServiceBus.Mvc'. 
> It uses an action filter to automatically send messages when a request is successful in your mvc project. 

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
    
    services.RegisterServiceBusDispatch().ToTopic("mytopic", builder =>
    {
        // The default name for this dispatch is "WeatherForecast"
        builder.RegisterDispatch<WeatherForecast>()
               .CustomizePayloadTypeId("Forecast");
    });
}
```

#### Customizing the outgoing message

You can change or set information on an ongoing message right before it is sent by calling `.CustomizeOutgoingMessage()` :
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Initialize serviceBus
    
    services.RegisterServiceBusDispatch().ToTopic("mytopic", builder =>
    {
        builder.RegisterDispatch<WeatherForecast>()
               .CustomizeOutgoingMessage((message, payload) =>
               {
                    // message is the created message ready to be sent
                    // payload is the object the message was created from
               });
    });
}
```
