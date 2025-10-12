# Ev.ServiceBus

This is a wrapper around [Azure Messaging Service Bus](https://www.nuget.org/packages/Azure.Messaging.ServiceBus/)

Its goal is to make it the easiest possible to send and receive ServiceBus messages in an [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/?view=aspnetcore-2.2) project.

## Base concept

This NuGet revolves around declaring [POCOs](https://en.wikipedia.org/wiki/Plain_old_CLR_object) 
as message's payload contracts as well as registering their corresponding reception handlers.

To send a message, you then only need to instantiate said contract and publish it. 
The nuget will take care of serializing the object into a message and send it to the correct queue or topic. 

To receive a message, you register a handler that will receive a single type of message. 
The nuget will take care of deserializing the message into proper object and give it to the corresponding handler.

### Underlying protocol

This whole process revolves around the ability to differentiate messages by their payload types. 
To do that, it adds a UserProperty to every message. This property is the unique identifier for a designated contract 
across the whole system (name of the property : `PayloadTypeId`). 

> Be careful when you have a system with several applications. If several applications send 2 different contracts 
> with the same Id to a single queue/topic, the receiving handlers will not be able to differentiate between them.

## Primary features

- Automatic handling of the QueueClient, TopicClient and SubscriptionClient lifecycle (the resources will be created and disposed when the application launches and shuts down).
- Ability to deactivate reception and/or sending of messages application wide by configuration.
- Automatic Serialization and Deserialization of incoming and outgoing messages.
- Message reception handling integrated with [ASP.NET Core IOC container](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/) :
  - Ability to register handlers to take care of messages.
  - A service provider scope is created for each reception of messages.
  - Basic logging during reception of message.

## Summary

* [Changelog](./docs/CHANGELOG.md)
* [Initial Set up](./docs/SetUp.md)
* [How to send messages](./docs/SendMessages.md)
* [How to receive messages](./docs/ReceiveMessages.md)
* [Isolation Feature](./docs/Isolation.md)
* [Advanced scenarios](./docs/AdvancedScenarios.md)
* [Ev.ServiceBus.HealthChecks](./docs/Ev.ServiceBus.HealthChecks.md)
* [Instrumentation](./docs/Instrumentation.md)
* [Ev.ServiceBus.Mvc](./docs/Ev.ServiceBus.Mvc.md)

## Secondary projects
