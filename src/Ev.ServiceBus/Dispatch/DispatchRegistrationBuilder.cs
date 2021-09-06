using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace Ev.ServiceBus.Dispatch
{
    public class DispatchRegistrationBuilder
    {
        private readonly IServiceCollection _services;
        private readonly ClientOptions _options;

        public DispatchRegistrationBuilder(IServiceCollection services, ClientOptions options)
        {
            _services = services;
            _options = options;
        }

        /// <summary>
        /// Sets a specific connection for the underlying resource.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="receiveMode"></param>
        /// <param name="retryPolicy"></param>
        public void CustomizeConnection(
            string connectionString,
            ReceiveMode receiveMode = ReceiveMode.PeekLock,
            RetryPolicy? retryPolicy = null)
        {
            _options.WithConnection(connectionString, receiveMode, retryPolicy);
        }

        /// <summary>
        /// Registers a class as a payload to serialize and send through the current resource.
        /// </summary>
        /// <typeparam name="TDispatchModel">The class to serialize the message into</typeparam>
        /// <returns></returns>
        public MessageDispatchRegistration RegisterDispatch<TDispatchModel>()
        {
            var builder = new MessageDispatchRegistration(_options, typeof(TDispatchModel));
            _services.Configure<ServiceBusOptions>(options =>
            {
                options.RegisterDispatch(builder);
            });
            return builder;
        }
    }
}
