# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
### Changed
### Removed

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
    options.RegisterQueue("MyQueue")
        .WithConnection(new ServiceBusConnection(""))
        .WithReceiveMode(ReceiveMode.PeekLock)
        .WithRetryPolicy(RetryPolicy.NoRetry);

    options.RegisterTopic("MyTopic")
        .WithConnectionStringBuilder(new ServiceBusConnectionStringBuilder())
        .WithRetryPolicy(RetryPolicy.NoRetry);

    options.RegisterSubscription("MyTopic", "MySubscription")
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
