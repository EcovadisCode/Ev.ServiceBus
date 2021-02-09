using System;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ev.ServiceBus.IntegrationEvents.Subscription
{
    public class ReceptionRegistrationBuilder
    {
        private readonly ReceiverOptions _options;
        private readonly IServiceCollection _services;

        public ReceptionRegistrationBuilder(IServiceCollection services, ReceiverOptions receiverOptions)
        {
            _services = services;
            _options = receiverOptions;
        }

        public void CustomizeMessageHandling(int maxConcurrentCalls = 1, TimeSpan maxAutoRenewDuration = default)
        {
            _options.ToIntegrationEventHandling(maxConcurrentCalls, maxAutoRenewDuration);
        }

        public void CustomizeConnection(string connectionString,
            ReceiveMode receiveMode = ReceiveMode.PeekLock,
            RetryPolicy? retryPolicy = null)
        {
            _options.WithConnection(connectionString, receiveMode, retryPolicy);
        }

        public MessageReceptionRegistration RegisterReception<TReceptionModel, THandler>()
            where THandler : class, IIntegrationEventHandler<TReceptionModel>
        {
            _services.TryAddScoped<THandler>();
            var builder = new MessageReceptionRegistration(_options, typeof(TReceptionModel), typeof(THandler));
            _services.AddSingleton(builder);
            return builder;
        }
    }
}
