# Isolation feature

## Concept

In a microservice ecosystem where those services communicate through messages via Service bus queues or topics, some scenarios become difficult to debug.
For example, sometimes you want to manually send a message to a queue and debug in local how your microservice behaves. That would require you to recreate a service bus namespace with the proper queue configured and link app running in local to it. 
Another example is, sometimes you want to send a message to one microservice to see and debug how another microservice behaves down the line.

The isolation feature allows your local application to connect to the service bus namespace of an up and running environment without disturbing it's stability.
It then allows you to manually send messages with some metadata to that queue and you local application will receive it instead of the app running in the environment.

## Configuration

### Default configuration
By default, the isolation feature is disabled. your application will treat every incoming messages.
```csharp
services.AddServiceBus(
    settings =>
    {
        // Provide a connection string here !
        settings.WithConnection("Endpoint=sb://yourconnection.servicebus.windows.net/;SharedAccessKeyName=yourkeyh;SharedAccessKey=ae6pTuOBAFDH2y7xJJf9BFubZGxXMToN6B9NiVgLnbQ=", new ServiceBusClientOptions());
        settings.WithIsolation(IsolationBehavior.HandleAllMessages, null, null);    
    })
```

### Environment configuration
To use the feature, you need to activate isolation both on your local app and on your environment.

Applications running in your environment must use the "HandleNonIsolatedMessages" behavior.
You also need to provide a name for your microservice.

```csharp
services.AddServiceBus(
    settings =>
    {
        // Provide a connection string here !
        settings.WithConnection("Endpoint=sb://yourconnection.servicebus.windows.net/;SharedAccessKeyName=yourkeyh;SharedAccessKey=ae6pTuOBAFDH2y7xJJf9BFubZGxXMToN6B9NiVgLnbQ=", new ServiceBusClientOptions());
        settings.WithIsolation(IsolationBehavior.HandleNonIsolatedMessages, null, "My.Application");    
    })
```

### Local configuration
To use the feature, you need to activate isolation both on your local app and on your environment.

The Application running in local must use the "HandleIsolatedMessage" behavior.
You also need to provide a name for your microservice and an isolation key.
Your local application will only process messages that have the same isolation key as the one you provide.

```csharp
services.AddServiceBus(
    settings =>
    {
        // Provide a connection string here !
        settings.WithConnection("Endpoint=sb://yourconnection.servicebus.windows.net/;SharedAccessKeyName=yourkeyh;SharedAccessKey=ae6pTuOBAFDH2y7xJJf9BFubZGxXMToN6B9NiVgLnbQ=", new ServiceBusClientOptions());
        settings.WithIsolation(IsolationBehavior.HandleIsolatedMessage, "My.IsolationKey", "My.Application");    
    })
```

## Usage

### Simple case
Now to use the feature, you will need to send a message with the proper metadata to the queue/topic of your environment.

Let's say you have a microservice `App1` and you want to run that application on local for debugging purposes.
You will need to send a message to the queue/topic of `App1` with the following metadata:
```json
{
    "IsolationKey": "My.IsolationKey",
    "ApplicationName": "App1"
}
```
Your local app will receive the message and process it.

### Complex case
Now let's say that in your environment you have the following microservices: `App1`, `App2`, `App3`, `App4`, `App5`.
They communicate with each other through messages. 
The entry point of your ecosystem is `App1`, but you want to debug `App2` and `App3` locally.

You will need to send a message to the queue/topic of `App1` with the following metadata:
```json
{
    "IsolationKey": "My.IsolationKey",
    "ApplicationName": "App2,App3"
}
```
The isolation metadata will be transferred to any message sent during the processing of the isolated message.

Meaning that your local apps will be able to receive subsequent isolated messages allowing you to debug them.
