using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Ev.ServiceBus
{
    public class ServiceBusBuilder
    {
        public IServiceCollection Services { get; }

        public ServiceBusBuilder(IServiceCollection services)
        {
            Services = services;
        }

        /// <summary>
        /// Registers a listener that will be called each time the execution of a message starts, has succeed or fails.
        /// </summary>
        /// <typeparam name="TEventListener"></typeparam>
        /// <returns></returns>
        public ServiceBusBuilder RegisterEventListener<TEventListener>() where TEventListener : class, IServiceBusEventListener
        {
            Services.AddScoped<IServiceBusEventListener, TEventListener>();
            return this;
        }
    }
}
