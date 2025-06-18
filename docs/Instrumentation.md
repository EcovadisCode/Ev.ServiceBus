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

# How to add Metrics

Ev.ServiceBus includes built-in metrics support through `ServiceBusMeter`, which provides the following metrics:

- `ev.servicebus.messages.sent`: Counter for total number of messages sent to the service bus
- `ev.servicebus.messages.received`: Counter for total number of messages received from the service bus
- `ev.servicebus.messages.delivery.count`: Histogram tracking delivery attempts for a message
- `ev.servicebus.message.queue.latency`: Histogram measuring time a message spends in queue (ms)

These metrics include labels: `clientType`, `resourceId`, and `payloadTypeId` for detailed analysis.

## Using with OpenTelemetry

To collect these metrics with OpenTelemetry, add the `Ev.ServiceBus` meter to your OpenTelemetry configuration:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Ev.ServiceBus");
    });
```

## Using with Prometheus

To export metrics to Prometheus:

1. Install the `Ev.ServiceBus.Prometheus` package
2. Register the `ServicebusPrometheusExporter` as a hosted service:

```csharp
builder.Services.AddHostedService<ServicebusPrometheusExporter>();
```

3. Configure the Prometheus HTTP endpoint in your application:

```csharp
app.UseMetricServer();
app.UseHttpMetrics();
```

This will expose your metrics at the default `/metrics` endpoint that can be scraped by Prometheus.
