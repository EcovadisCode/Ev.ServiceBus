using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Listeners;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Dispatch;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.Reception;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: InternalsVisibleTo("Ev.ServiceBus.UnitTests")]
[assembly: InternalsVisibleTo("Ev.ServiceBus.TestHelpers")]

namespace Ev.ServiceBus;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers basic services for using ServiceBus.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config">Lambda expression to configure Service Bus</param>
    /// <typeparam name="TMessagePayloadSerializer">Type of the serializer to use.</typeparam>
    /// <returns></returns>
    public static ServiceBusBuilder AddServiceBus(
        this IServiceCollection services,
        Action<ServiceBusSettings> config)
    {
        RegisterBaseServices(services);

        services.AddSingleton<ITransactionManager, DefaultTransactionManager>();
        services.TryAddSingleton<IMessagePayloadSerializer, TextJsonPayloadSerializer>();
        services.Configure<ServiceBusOptions>(
            options =>
            {
                config(options.Settings);
            });

        return new ServiceBusBuilder(services);
    }

    private static void RegisterBaseServices(IServiceCollection services)
    {
        services.AddLogging();

        RegisterResourceManagementServices(services);

        RegisterMessageDispatchServices(services);

        RegisterMessageReceptionServices(services);

        services.TryAddScoped<IMessageMetadataAccessor, MessageMetadataAccessor>();
    }

    private static void RegisterMessageReceptionServices(IServiceCollection services)
    {
        services.TryAddScoped<MessageReceptionHandler>();
    }

    private static void RegisterMessageDispatchServices(IServiceCollection services)
    {
        services.TryAddScoped<MessageDispatcher>();
        services.TryAddScoped<IMessagePublisher>(provider => provider.GetRequiredService<MessageDispatcher>());
        services.TryAddScoped<IMessageDispatcher>(provider => provider.GetRequiredService<MessageDispatcher>());
        services.TryAddScoped<IDispatchSender, DispatchSender>();
    }

    private static void RegisterResourceManagementServices(IServiceCollection services)
    {
        services.TryAddSingleton<ServiceBusRegistry>();
        if (services.Any(o => o.ServiceType == typeof(IServiceBusRegistry)) == false)
        {
            services.AddSingleton<IServiceBusRegistry, ServiceBusRegistry>(provider =>
                provider.GetRequiredService<ServiceBusRegistry>());
        }

        services.TryAddSingleton<ServiceBusEngine>();
        services.TryAddSingleton<MessageSenderFactory>();
        services.TryAddSingleton<ReceiverWrapperFactory>();
        services.TryAddSingleton<IClientFactory, ClientFactory>();

        if (services.Any(o => o.ImplementationType == typeof(ServiceBusHost)) == false)
        {
            services.AddHostedService<ServiceBusHost>();
        }
    }

    /// <summary>
    /// The start of the registration process for message reception
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static ReceptionBuilder RegisterServiceBusReception(this IServiceCollection services)
    {
        RegisterBaseServices(services);
        return new(services);
    }

    /// <summary>
    /// The start of the registration process for message dispatch
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static DispatchBuilder RegisterServiceBusDispatch(this IServiceCollection services)
    {
        RegisterBaseServices(services);
        return new(services);
    }
}