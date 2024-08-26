# How to add Instrumentation for message handling

By default, our communication Ev.ServiceBus includes OpenTelemetry instrumentation utilizing `ActivitySource` from `System.Diagnostics`. To enable this, you simply need to add the `ActivitySource` to your configuration.

```csharp

var appBuilder = WebApplication.CreateBuilder(args);

appBuilder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder.AddSource("Ev.ServiceBus"));
```

Alternatively, you can use Elastic APM for instrumentation by utilizing the Ev.ServiceBus.Apm as following.

```csharp

  services.AddServiceBus(settings => {
        settings.Enabled = true;
        settings.ReceiveMessages = true;
        settings.WithConnection("", new ServiceBusClientOptions());
    }).UseApm();

```