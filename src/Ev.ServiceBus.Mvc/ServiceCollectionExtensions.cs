using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Ev.ServiceBus.Mvc
{
    public static class ServiceCollectionExtensions
    {
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
}
