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

        public void CustomizeConnection(
            string connectionString,
            ReceiveMode receiveMode = ReceiveMode.PeekLock,
            RetryPolicy? retryPolicy = null)
        {
            _options.WithConnection(connectionString, receiveMode, retryPolicy);
        }

        public MessageDispatchRegistration RegisterDispatch<TDispatchModel>()
        {
            var builder = new MessageDispatchRegistration(_options, typeof(TDispatchModel));
            _services.AddSingleton(builder);
            return builder;
        }
    }
}
