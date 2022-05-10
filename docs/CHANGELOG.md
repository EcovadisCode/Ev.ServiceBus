# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
### Changed
### Removed

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
