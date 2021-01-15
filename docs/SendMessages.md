# How to send messages

To start sending messages to `queues` or/and `topics` let's register them first.

### Configure Services

The registration process is very simple:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceBus(settings => {
        settings.WithConnection(serviceBusConnectionString);
    });
    
    services.RegisterServiceBusQueue("QueueName");
    
    services.RegisterServiceBusTopic("TopicName");
}
```
The above example is the minimum to register for sending messages.
The provided names (`TopicName` or `QueueName`) must be the names of resources present on your [Azure Service Bus namespace](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview#namespaces)

### Sending messages

To receive messages you need to use a `IServiceBusRegistry` instance:
```csharp
var registry = _serviceProvider.GetService<IServiceBusRegistry>();
var queue = registry.GetQueueSender("QueueName");
var topic = registry.GetTopicSender("TopicName");

queue.SendAsync(new Message(/* */)); // send one
queue.SendAsync(new List<Message> { /* */ }); // send many
var sequence = queue.ScheduleMessageAsync(new Message(/* */), DateTimeOffset.Now.AddDays(1)); // schedule sending
queue.CancelScheduledMessageAsync(sequence); // cancel it

// for the topic it looks the same
```

### Read More
Official Microsoft tutorials:
- [Sending Messages to a Queue](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues#send-messages-to-the-queue)
- [Sending Messages to a Topic](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-how-to-use-topics-subscriptions#send-messages-to-the-topic)
