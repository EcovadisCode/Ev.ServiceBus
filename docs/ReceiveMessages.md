# How to receive messages

To start receiving messages from `queues` or/and `subscriptions` let's register them first.

### Configure Services

The registration process is very simple:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceBus(settings => {
        settings.WithConnection(serviceBusConnectionString);
    });

    options.RegisterSubscription("TopicName", "SubscriptionName")
        .WithCustomMessageHandler<SubscriptionHandler>(/*options*/);

    options.RegisterQueue("QueueName")
        .WithCustomMessageHandler<QueueHandler>(/*options*/);
}
```
The above example is the minimum to register for receiving messages.
You need to add names (`TopicName` and `SubscriptionName` or `QueueName`), the connection string to the service bus you created in the Azure
and the class implementing the `IMessageHandler` interface.

### Handling messages

To handle messages you need to implement the `IMessageHandler`:
```csharp
public class SubscriptionHandler : IMessageHandler
{
    public async Task HandleMessageAsync(MessageContext context)
    {
        var message = context.Message; // Microsoft.Azure.ServiceBus.Message
        /*        Parse a message code        */
        if (!ok)
            throw new InvalidOperationException();
    }
}
```
To read more about the `Message` class [check here](https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.servicebus.message?view=azure-dotnet).


### Handling exceptions
To handle exceptions you need to register it first:
```csharp
options.RegisterQueue("QueueName")
       .WithCustomExceptionHandler<ExceptionHandler>();

```
The `ExceptionHandler` class must implement the `IExceptionHandler` interface:

```csharp
public class QueueMessageErrorHandler : IExceptionHandler
{
    public Task HandleExceptionAsync(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
    {
        /* Code */
    }
}
```
About the `ExceptionReceivedEventArgs` you can read [here](https://docs.microsoft.com/en-us/dotnet/api/microsoft.servicebus.messaging.exceptionreceivedeventargs?view=azure-dotnet).

### Read More
Official Microsoft tutorials:
- [Receiving Messages from a Queue](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues#receive-messages-from-the-queue)
- [Receiving Messages from a Subscription](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-how-to-use-topics-subscriptions#receive-messages-from-the-subscription)
