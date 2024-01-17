using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Ev.ServiceBus.Mvc;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Sets up integration services with Ev.ServiceBus for MVC components
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IMvcBuilder AddServiceBusMvcIntegration(this IMvcBuilder builder)
    {
        builder.Services.AddScoped<ServiceBusDispatcherFilter>();
        builder.Services.Configure<MvcOptions>(options =>
        {
            options.Filters.AddService<ServiceBusDispatcherFilter>();
        });

        return builder;
    }
}