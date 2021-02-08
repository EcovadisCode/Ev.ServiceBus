using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ev.ServiceBus.IntegrationEvents.Subscription
{
    public class ReceptionRegistrationBuilder
    {
        private readonly IServiceCollection _services;

        public ReceptionRegistrationBuilder(IServiceCollection services, ReceiverOptions receiverOptions)
        {
            _services = services;
            Options = receiverOptions;
        }

        public ReceiverOptions Options { get; }

        public MessageReceptionRegistration RegisterReception<TReceptionModel, THandler>()
            where THandler : class, IIntegrationEventHandler<TReceptionModel>
        {
            _services.TryAddScoped<THandler>();
            var builder = new MessageReceptionRegistration(Options, typeof(TReceptionModel), typeof(THandler));
            _services.AddSingleton(builder);
            return builder;
        }
    }
}
