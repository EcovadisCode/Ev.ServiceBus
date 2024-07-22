using Ev.ServiceBus.Abstractions.Listeners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ev.ServiceBus.Apm;

public static class ServiceBusBuilderExtensions
{
    public static ServiceBusBuilder UseApm(this ServiceBusBuilder builder)
    {
        builder.Services.RemoveAll<ITransactionManager>();
        builder.Services.AddSingleton<ITransactionManager, ApmTransactionManager>();
        return builder;
    }
}