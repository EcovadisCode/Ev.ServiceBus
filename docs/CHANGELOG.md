# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 5.3.1
- Changed
  - Fix usage of sent counter on receiver instead of received counter
## 5.3.0
- Added
  - Introduced Isolation mode for receiving messages. Instances with different key will ignore the message not intended for them.
  - New metrics to monitor service bus performance:
    - ev.servicebus.messages.sent : Total number of messages sent to the service bus.
    - ev.servicebus.messages.received : Total number of messages received from the service bus.
    - ev.servicebus.messages.delivery.count : Number of deliveries attempted for a single message. Incremented when a message lock expires or the message is explicitly abandoned by the receiver.
    - ev.servicebus.message.queue.latency : Time a message spends in the queue from enqueue (by sender) until delivery to the receiver for processing (milliseconds).
- Changed
  - Updated Azure.Messaging.ServiceBus to version 7.20.1
  - Updated AspNetCore.HealthChecks.AzureServiceBus to version 8.0.1
- Removed
  - Removed net6.0 target framework.
## 5.2.0
- Added
  - Introduced SendDispatch methods on DispatchSender. Those methods allow to send single message bigger than 1MB

## 5.1.5
- Changed
  - skip transaction renaming when ServiceBus instrumentation is missing

## 5.1.4
- Changed
  - Fix diagnostic id null reference

## 5.1.3
- Changed
  - Fix logging scope formatting
  - Add span with span link to the process transaction to have link with the producer

## 5.1.2
- Open telemetry
  - Fix Transaction to follow Open telemetry standard with Span links

## 5.1.1
- Open telemetry
  - Fix a problem where the nuget Ev.ServiceBus.Apm was not produced

## 5.1.0
- Added
  - MessageReceptionBuilder
    - Exposed `HandlerType` in `MessageReceptionBuilder`.
    - Exposed `PayloadType` in `MessageReceptionBuilder`.
  - new categorized Logging 
    - Ev.ServiceBus.LoggingExtensions.MessageProcessing : For logs related to message processing
    - Ev.ServiceBus.LoggingExtensions.ServiceBusClientManagement : For logs related to creation of client / disposition of clients
    - Ev.ServiceBus.LoggingExtensions.ServiceBusEngine : For logs related to the initialization of the Host
    - Ev.ServiceBus.HealthChecks.LoggingExtensionsHealthChecks : For logs related to service bus health checks registration
  - New telemetryOptions
    - ActivitySource (Ev.ServiceBus) for message processing 
    - Ev.ServiceBus.Apm : Elastic Apm integration
- Modified 
  - Reduce number of log entries and duplicate exception logging 
  - Use of high performance logging mechanism

`[Breaking]`
The exception catched during IMessageHandler was changed to FailedToProcessMessageException
Original exception is stored in the inner exception if you are using IExceptionHandler use this to get original message

```csharp
public class CustomExceptionHandler : IExceptionHandler
{
    public Task HandleExceptionAsync(ProcessErrorEventArgs args)
    {
            var original = exceptionEvent.Exception is FailedToProcessMessageException wrapperException
                ? wrapperException.InnerException!
                : exceptionEvent.Exception!;
        return Task.CompletedTask;
    }
}
```

## 5.0.0
- Removed obsolete methods and related code :
  - `services.RegisterServiceBusQueue("queueName");`
  - `services.RegisterServiceBusTopic("topicName");`
  - `services.RegisterServiceBusSubscription("topicName", "subscriptionName");`
- Ev.ServiceBus doesn't require you to define a payload serializer anymore. By default, System.Text.Json will be used as a serializer.
- Added `services.AddServiceBus().WithPayloadSerializer<TMessagePayloadSerializer>();` that allows you to specify a serializer.

## 4.12.0
- Added .NET 8 support
- Changed - Azure.ServiceBus nuget updated to v 7.17.0

## 4.11.1
- Fixed a bug about .AddEvServiceBusChecks(), if didn't you call that before any other code that registers IConfigureOptions<HealthCheckServiceOptions>, it would not be registered.

## 4.11.0
- Improved `IMessagePublisher.Publish<TMessagePayload>(TMessagePayload messageDto, Action<IDispatchContext> messageContextConfiguration)` method to be able to set custom application properties to a dispatch

## 4.10.0
- IMessageDispatcher.ExecuteDispatches method has now CancellationToken support.
- Added IDispatchExtender interface that allows you to access and update outgoing message just before they are sent.

## 4.9.0
- Refactored internals of Ev.ServiceBus to put MessageReceptionRegistration in the MessageContext.
- MessageReceptionRegistration is now accessible in the IServiceBusEventListener interface.

## 4.8.1
- Changed - Azure.ServiceBus nuget updated to v 7.12.0

## 4.8.0
- Added DiagnosticID (traceparent) support for publisher / dispatcher in case of end to end tracing (according: [Distributed tracing and correlation through Service Bus messaging](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-end-to-end-tracing?tabs=net-standard-sdk-2))  

## 4.7.0
- Added the MessageId property to the MessageContext class to allow MessageId customization on publish

## 4.6.1
- Made some internal methods public in IServiceBusRegistry class

## 4.6.0
- Changed the dispatch Sender to use ServiceBusMessageBatch and optimize message sending. 

## 4.5.0
- Added methods to Schedule dispatches
- Removed usages of netcore3.1 and net5 frameworks.

## 4.4.1
- made the registration of resources case insensitive.

## 4.4.0
- Update the `IMessageMetadata` interface to expose all metadata properties of the receiving message as well as the methods to complete/abandon/deadletter/defer said message.

## 4.3.1
- Resolved a bug where putting a '/' in PayloadTypeIds of message contracts will make the AsyncUi json not valid.

## 4.3.0
- Removed EventTypeId related code.
- Healthchecks are now disabled when Ev.ServiceBus is disabled.
- Connections won't be created anymore when Ev.ServiceBus is disabled.

## 4.2.0
- Added method overloads for `RegisterDispatch` and `RegisterReception` Which are not templated.
- Made old registration method obsolete.

## 4.1.1
- merged v3.7.0 with v4.1.0

## 4.1.0
- CorrelationId is now created automatically and is being passed to newly published events.
- Added method overload enabling you to publish a message with a specific correlationId.

## 4.0.0
- refactored the entire project to use [Azure.Messaging.ServiceBus](https://www.nuget.org/packages/Azure.Messaging.ServiceBus/)

## 3.7.0
- Made the matching of payloadTypeIds less strict by making it case-insensitive.

## 3.6.0
- Added method overloads enabling you to publish a message with a specific sessionId.

## 3.5.0
- Clients can now receive message in session mode.

## 3.4.0
- Added net6.0 as target framework.
- Added IMessageMetadataAccessor service that allows you to access data like correlationId 
or user properties of the underlying message. 

## 3.3.0
- Added `Ev.ServiceBus.AsyncApi` package. It helps you generate an AsyncApi schema with Ev.ServiceBus registrations. 

## 3.2.0
- Added log messages that tells if Ev.ServiceBus is deactivated or if only reception is deactivated.
- Ev.ServiceBus.HealthChecks is now compatible with netcore3.1
- Refactored internal registration of dispatches and receptions.
- Added `IServiceBusEventListener` interface that lets you hook up to the internal events `start`, `succeeded` and `failed`.

## 3.1.0

- Add custom tags for health check

## 3.0.1

- Fixed an issue where messages were not sent anymore on the dead letter queue

## 3.0.0

### Added

- The main Api has been redesigned to reduce the number of lines of code necessary to configure it.
  
  Example of old API :
```csharp
services.ConfigureServiceBus(options =>
{
  options.RegisterSubscription("mytopic", "mysubscription")
         .WithConnectionString(serviceBusConnectionString)
         .ToIntegrationEventHandling();
});

services.RegisterIntegrationEventSubscription<MyEvent1, MyEvent1Handler>(builder =>
{
    builder.EventTypeId = "MyEvent1";
    builder.ReceiveFromSubscription("mytopic", "mysubscription");
});

services.RegisterIntegrationEventSubscription<MyEvent2, MyEvent2Handler>(builder =>
{
    builder.EventTypeId = "MyEvent2";
    builder.ReceiveFromSubscription("mytopic", "mysubscription");
});

services.RegisterIntegrationEventSubscription<MyEvent3, MyEvent3Handler>(builder =>
{
    builder.EventTypeId = "MyEvent3";
    builder.ReceiveFromSubscription("mytopic", "mysubscription");
});
```
  Example of new API :
```csharp
services.RegisterServiceBusReception().FromSubscription("mytopic", "mysubscription", builder => {
  builder.RegisterReception<MyEvent1, MyEvent1Handler>();
  builder.RegisterReception<MyEvent2, MyEvent2Handler>();
  builder.RegisterReception<MyEvent3, MyEvent3Handler>();
});
```
- Added the ability to automatically serialize objects into messages and send them to a topic or queue depending on configuration.
- Added the ability to automatically deserialize messages into objects that are received and process them into specific handlers depending on configuration.
- The PayloadTypeId is now automatically generated and uses the type's simple name.
- Queue are now fully supported for message reception.
- Registration of underlying senders/receivers is now automatic.
- The NuGet packages now properly target multiple frameworks including net5
- The protocol now uses the user property named 'PayloadTypeId' instead of 'EventTypeId' to resolve routing (The old user property is still provided for retro-compatibility purposes).
- Added logging messages when a message is sent.
- Upon reception of a message, a log scope is now created to pass useful information (such as the name of the resource the received message is coming from)
- Added Ev.ServiceBus.HealthChecks NuGet package : it's goal is to automatically register Ev.ServiceBus registrations as healthCheck registrations.
- Added Ev.ServiceBus.Mvc NuGet package : it's goal is to better integrate Ev.ServiceBus with MVC components of Asp.Net Core
  
## 2.0.0 - 2021-01-07
- The method `services.ConfigureServiceBus()` has been replaced:
  ```csharp
      // this method has been removed
      public static IServiceCollection ConfigureServiceBus(this IServiceCollection services, Action<ServiceBusOptions> config);
      
      // and replaced by these
      public static QueueOptions RegisterServiceBusQueue(this IServiceCollection services, string queueName);
      public static TopicOptions RegisterServiceBusTopic(this IServiceCollection services, string topicName);
      public static SubscriptionOptions RegisterServiceBusSubscription(this IServiceCollection services, string topicName, string subscriptionName);
  ```
- The methods used to set connection settings on a registration have also been replaced : 
  ```csharp
  // these methods have been remmoved
  public static TOptions WithConnectionString<TOptions>(this TOptions options, string connectionString) where TOptions : ClientOptions;
  public static TOptions WithConnection<TOptions>(this TOptions options, ServiceBusConnection connection);
  public static TOptions WithConnectionStringBuilder<TOptions>(this TOptions options, ServiceBusConnectionStringBuilder connectionStringBuilder) where TOptions : ClientOptions;
  public static TOptions WithReceiveMode<TOptions>(this TOptions options, ReceiveMode receiveMode);
  public static TOptions WithRetryPolicy<TOptions>(this TOptions options, RetryPolicy retryPolicy);
  
  // and replaces by these
  public static TOptions WithConnection<TOptions>(this TOptions options, string connectionString, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy? retryPolicy = null) where TOptions : ClientOptions;
  public static TOptions WithConnection<TOptions>(this TOptions options, ServiceBusConnection connection, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy? retryPolicy = null) where TOptions : ClientOptions;
  public static TOptions WithConnection<TOptions>(this TOptions options, ServiceBusConnectionStringBuilder connectionStringBuilder, ReceiveMode receiveMode = ReceiveMode.PeekLock, RetryPolicy? retryPolicy = null) where TOptions : ClientOptions;
  ```
- Refactored the `services.AddServiceBus()` to be more extensible :
  ```csharp
  // old method
  public static IServiceCollection AddServiceBus(this IServiceCollection services, bool enabled = true, bool receiveMessages = true);
  // new method
  public static IServiceCollection AddServiceBus(this IServiceCollection services, Action<ServiceBusSettings> config);
  ```
- Added the ability to set default connection settings : 
  ```
  services.AddServiceBus(settings => {
    settings.WithConnection(connectionString);
  });
  ```
- All log messages are now prefixed with "[Ev.ServiceBus]"
- Microsoft.Azure.ServiceBus NuGet package has been updated to v5.1.0
- The method `.WithCustomMessageHandler()` now registers the handler into the IOC container (before you needed to register it yourself)

## 1.4.2 - 2020-09-30
- Added `.ConfigureAwait(false)` to async calls.

## 1.4.1 - 2020-07-14
- added log for message processing time

## 1.4.0 - 2020-05-27
- removed closing behavior when an exception occurs.

## 1.3.1 - 2020-05-19

### Changed
- Documentation
- Moving around a bit of code. No functionality changed.

## 1.3.0 - 2020-05-12

### Added
- Added a boolean `Enabled` that controls whether Ev.ServiceBus is activated or not in the server.
- Added a boolean `ReceiveMessages` that controls Ev.ServiceBus will listen or not to messages.
### Changed
### Removed

## 1.2.0 - 2020-02-25

### Changed
- The exception handling during reception of a message has drastically changed. Now it will close the client at the first exception received by the exception handler.
    - A control parameter at exception handler execution allows for disabling this feature.
    - This change has been done to prevent the client from flooding the logs with repetitive errors.
    - A critical log is produced whenever a client is closed that way.
    - The application will need to be restarted to resolve the issue.

## 1.1.1 - 2020-02-07

### Added
- Now the nuget is built with several target platforms instead of just netstandard2.0:
    - netstandard2.0
    - net48
    - netcoreapp2.2
    - netcoreapp3
- `services.AddServiceBus();` method can now be called several times without harm
- updated nuget `Microsoft.Azure.ServiceBus` to version 4.1.1.

## 1.1.0 - 2019-09-02

### Added

- Added `WithConnection` method that allows you to provide a `ServiceBusConnection` object instead of a connection string.
- Added `WithConnectionStringBuilder` method that allows you to provide `ServiceBusConnectionStringBuilder` object instead of a connection string.
- Added `WithReceiveMode` method that allows you to set the [ReceiveMode](https://docs.microsoft.com/en-us/dotnet/api/microsoft.servicebus.messaging.receivemode?view=azure-dotnet) for the registered clients.
- Added `WithRetryPolicy` method that allows you to set the retry policy for the registered clients.

Examples:
```csharp
services.ConfigureServiceBus(options =>
{
    options.RegisterServiceBusQueue("MyQueue")
        .WithConnection(new ServiceBusConnection(""))
        .WithReceiveMode(ReceiveMode.PeekLock)
        .WithRetryPolicy(RetryPolicy.NoRetry);

    options.RegisterServiceBusTopic("MyTopic")
        .WithConnectionStringBuilder(new ServiceBusConnectionStringBuilder())
        .WithRetryPolicy(RetryPolicy.NoRetry);

    options.RegisterServiceBusSubscription("MyTopic", "MySubscription")
        .WithConnectionString("")
        .WithReceiveMode(ReceiveMode.ReceiveAndDelete)
        .WithRetryPolicy(RetryPolicy.NoRetry);
});
```


### Changed
### Removed

## 1.0.7 - 2019-08-21

### Added

- Added `ClientType` information in the `IMessageReceiver` interface.
- Added `ClientType` information in the `IMessageSender` interface.
- Added `GetQueueSender` in the `IServiceBusRegistry` interface.
- Added `GetTopicSender` in the `IServiceBusRegistry` interface.
- Added a mechanism that throttles the log of the `MessagingEntityNotFoundException` exception.

### Changed

- `QueueSenderNotFoundException` exception will now be thrown when you try to get a sender that is not in the registry.
- `TopicSenderNotFoundException` exception will now be thrown when you try to get a sender that is not in the registry.
- `DuplicateQueueRegistrationException` exception will now be thrown when you try to register the same resource twice.
- `DuplicateSubscriptionRegistrationException` exception will now be thrown when you try to register the same resource twice.
- `DuplicateTopicRegistrationException` exception will now be thrown when you try to register the same resource twice.

### Removed

- Removed `RegisterMessageHandler` methods from `IMessageReceiver` interface.
- Removed `GetMessageReceiver` method from the `IServiceBusRegistry` interface.
- Removed `GetMessageSender` method from the `IServiceBusRegistry` interface.