# Set-Up

## .Net Core Web Project
All you need to do to be sure that everything works as expected is the following code 
(remember to check about how Ev.ServiceBus is initialized [here](https://github.com/EcovadisCode/Ev.ServiceBus/blob/master/docs/SetUp.md)):
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddServiceBus(settings => {
        settings.WithConnection("SomeConnectionString");
    });
    services.AddIntegrationEventHandling<MyMessageBodyParser>();
}
```

Since we don't know in which format you want to serialize your message's payload, 
we will require you to implement a service inheriting from the `IMessageBodyParser` interface:
```csharp
public class MyMessageBodyParser : IMessageBodyParser
{
        public SerializationResult SerializeBody(object objectToSerialize) 
        {
            // Your serialization logic
            return new SerializationResult("contentType", content);
        }
        
        public object DeSerializeBody(byte[] content, Type typeToCreate)
        {
            // Your deserialization logic
        }
}
```
> This service will be added as singleton