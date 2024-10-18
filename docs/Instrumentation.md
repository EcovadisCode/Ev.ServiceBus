# How to add Instrumentation for message handling

By default, our communication Ev.ServiceBus uses Activity enrichment for OpenTelemetry instrumentation. this includes renaming the Activity name to
$"{ClientType}/{ResourceId}/{PayloadTypeId}"
Examples 
`Queue/MyQueueName/MyPayloadType`
`Subscription/MyTopicName/Subscriptions/MySubscriptionName/MyPayloadType`

Note : For our activity extension to work you need to configure your OpenTelemetry tracing to enable Azure Activity Source 

Alternatively, you can use Elastic APM for instrumentation by utilizing the Ev.ServiceBus.Apm as following.

```csharp

  services.AddServiceBus(settings => {
        settings.Enabled = true;
        settings.ReceiveMessages = true;
        settings.WithConnection("", new ServiceBusClientOptions());
    }).UseApm();

```
