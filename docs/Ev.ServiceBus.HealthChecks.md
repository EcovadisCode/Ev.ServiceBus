# Ev.ServiceBus.HealthChecks

This NuGet integrates Ev.ServiceBus with [AspNetCore.HealthChecks.AzureServiceBus](https://github.com/xabaril/AspNetCore.Diagnostics.HealthChecks).
Every resource registered within Ev.ServiceBus will be added as a health check for the application.

## Initialization

To make it work you just have to call `.AddEvServiceBusChecks()` when adding health checks.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Initialize ServiceBus
    
    // Setup health checks
    services.AddHealthChecks()
        .AddEvServiceBusChecks("Custom tag 1", "Custom Tag 2");
}
```
