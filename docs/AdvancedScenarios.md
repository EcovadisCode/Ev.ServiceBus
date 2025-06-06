﻿# Advanced scenarios

## Customizing the connection to service bus

The method `.WithConnection()` as several different signatures. With them you can create a connection using either :
- a connection string.
- a `ServiceBusConnection` object.
- a `ServiceBusConnectionStringBuilder` object.

You can also use the `.WithConnection()` method to set the 
[ReceiveMode](https://docs.microsoft.com/en-us/dotnet/api/microsoft.servicebus.messaging.receivemode?view=azure-dotnet) 
and [RetryPolicy](https://docs.microsoft.com/en-us/dotnet/api/microsoft.servicebus.retrypolicy?view=azure-dotnet) for that connection.

examples :
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceBus(settings => {
        settings.WithConnection(serviceBusConnectionString, ReceiveMode.ReceiveAndDelete);
        settings.WithConnection(new ServiceBusConnection(), ReceiveMode.PeekLock, new CustomRetryPolicy());
        settings.WithConnection(new ServiceBusConnectionStringBuilder());
    });
}
```

## Overriding the default connection

Generally, you only need one connection for your application to run service bus.
If, for some reason, you need to set another connection for a specific registration, 
you can override the default connection by calling `.WithConnection()` on the registration itself. 

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceBus(settings => {
        settings.WithConnection(serviceBusConnectionString);
    });

    services.RegisterServiceBusDispatch().ToQueue("myqueue", builder =>
    {
        builder.CustomizeConnection(ConnectionString2);
        builder.RegisterDispatch<WeatherForecast[]>();
    });

    services.RegisterServiceBusDispatch().ToTopic("mytopic", builder =>
    {
        builder.CustomizeConnection(ConnectionString3);
        builder.RegisterDispatch<WeatherForecast>();
    });
    
    services.RegisterServiceBusReception().FromQueue("myqueue", builder =>
    {
        builder.CustomizeConnection(ConnectionString4);
        builder.RegisterReception<WeatherForecast[], WeatherMessageHandler>();
    });

    services.RegisterServiceBusReception().FromSubscription("mytopic", "mysubscription",
        builder =>
        {
            builder.CustomizeConnection(ConnectionString5);
            builder.RegisterReception<WeatherForecast, WeatherEventHandler>();
        });
}
```

## Custom serialization of messages

By default, System.Text.Json is used to serialize and deserialize messages.
If you want to provide another serialization/deserialization logic you can do using the following method :

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddServiceBus(settings => { })
            .WithPayloadSerializer<NewtonsoftJsonPayloadSerializer>();
    }
}

public class NewtonsoftJsonPayloadSerializer : IMessagePayloadSerializer
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Formatting = Formatting.None,
        Converters =
        {
            new StringEnumConverter()
        },
        NullValueHandling = NullValueHandling.Ignore
    };

    public SerializationResult SerializeBody(object objectToSerialize)
    {
        var json = JsonConvert.SerializeObject(objectToSerialize, Formatting.None, Settings);
        return new SerializationResult("application/json", Encoding.UTF8.GetBytes(json));
    }

    public object DeSerializeBody(byte[] content, Type typeToCreate)
    {
        var @string = Encoding.UTF8.GetString(content);
        return JsonConvert.DeserializeObject(@string, typeToCreate, Settings)!;
    }
}
```


## Listening to internal events

You can register a listener class that will be called every time the execution of a message starts, is successful and/or has failed. 

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceBus(settings => {
        settings.WithConnection(serviceBusConnectionString);
    })
    .RegisterEventListener<ServiceBusEventListener>();
}

public class ServiceBusEventListener : IServiceBusEventListener
{
    public Task OnExecutionStart(ExecutionStartedArgs args)
    {
        return Task.CompletedTask;
    }

    public Task OnExecutionSuccess(ExecutionSucceededArgs args)
    {
        return Task.CompletedTask;
    }

    public Task OnExecutionFailed(ExecutionFailedArgs args)
    {
        return Task.CompletedTask;
    }
}
```

## Async Api schema generation with Saunter

With `Ev.ServiceBus.AsyncApi` package, you can populate your AsyncApi schema 
with resources and models registered by `Ev.ServiceBus`. 
You can visit [Saunter](https://github.com/tehmantra/saunter) or [asyncapi.com](https://www.asyncapi.com/) 
for more information about Async Api specifications.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddAsyncApiSchemaGeneration(options => {
        options.AsyncApi = new AsyncApiDocument()
        {
            Info = new Info("Receiver API", "1.0.0")
            {
                Description = "Sample receiver project",
            }
        };
    });

    services.AddServiceBus<MessagePayloadSerializer>(settings => {
        settings.WithConnection(serviceBusConnectionString);
    })
    .PopulateAsyncApiSchemaWithEvServiceBus();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapAsyncApiDocuments();
        endpoints.MapAsyncApiUi();
    });
}
```

## Distributed tracing and correlation through Service Bus messaging support in case of publish / dispatch mechanism

Distributed tracing and correlation through Service Bus messaging is automatically supported by the library. It is supported according assumptions described [here](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-end-to-end-tracing?tabs=net-standard-sdk-2).
While message publishing `treceparent` value is automatically get from `Activity.Current.Id` in case if any publication is executed in any Activity scope.
If do you need pass another value manually while the publication. You can do this by set `IMessageContext` - what is possible in by using one of overloaded `Publish` method of `IMessagePublisher` interface implementation.

```csharp
public class MyMessageSender
        {

            private readonly IMessagePublisher _eventPublisher;

            public MyMessageSender(IMessagePublisher eventPublisher)
            {
                _eventPublisher = eventPublisher;
            }

            public async Task PublishMyMessage(MyEvent @event, string myCustomDiagnosticsId)
            {
                _eventPublisher.Publish(
                    @event,
                    // here you can pas the function that will set your custom value of diagnostic id
                    context => context.DiagnosticId = myCustomDiagnosticsId);
            }
        }
```

## Running service instance in isolation

By "running in isolation" we mean ability to run an instance of microservice (MS) on local environment, when MS is using real queues and topics to communicate with other MSs
Simple case : App1 sending message to App2, which responds back with another message.
Desired isolation is in the following: If message comes from App1.Local, then the response to it should be processed also only by App1.Local. Same holds true from App1.Cloud - it should process responses that are only coming from cloud instances.

This is achived by simply proper condifuration (see sample project)
```csharp
services.AddServiceBus(
    settings =>
    {
        // Provide a connection string here !
        settings.WithConnection("Endpoint=sb://yourconnection.servicebus.windows.net/;SharedAccessKeyName=yourkeyh;SharedAccessKey=ae6pTuOBAFDH2y7xJJf9BFubZGxXMToN6B9NiVgLnbQ=", new ServiceBusClientOptions());
        settings.UseIsolation = true;
        settings.IsolationKey = "your-isolation-key";
    })
```