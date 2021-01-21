# Ev.ServiceBus

This is a wrapper around [Microsoft Azure Service Bus](https://github.com/Azure/azure-service-bus)

Its goal is to make it the easiest possible to connect and handle an Azure ServiceBus resource (Queues, Topics or Subscriptions) inside [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/?view=aspnetcore-2.2).

Primary features :
- Automatic handling of the QueueClient, TopicClient and SubscriptionClient lifecycle (the resources will be created and disposed when the application launches and shuts down).
- Ability to deactivate reception and/or sending of messages application wide by configuration.
- Message reception handling integrated with [ASP.NET Core IOC container](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/) :
    - Ability to register handlers to take care of messages.
    - A service provider scope is created for each reception of messages.
    - Basic logging during reception of message.

## Summary

* [Changelog](./docs/CHANGELOG.md)
* [Initial Set up](./docs/SetUp.md)
* [How to send messages](./docs/SendMessages.md)
* [How to receive messages](./docs/ReceiveMessages.md)
* [Advanced scenarios](./docs/AdvancedScenarios.md)
