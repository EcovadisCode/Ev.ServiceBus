using System;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ev.ServiceBus.Reception
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

        /// <summary>
        /// Change the settings of the underlying message handler.
        /// </summary>
        /// <param name="maxConcurrentCalls">The number of messages that can be processed in parallel</param>
        /// <param name="maxAutoRenewDuration">The maximum time allowed for the execution of one message</param>
        public void CustomizeMessageHandling(int maxConcurrentCalls = 1, TimeSpan? maxAutoRenewDuration = null)
        {
            _options.ToMessageReceptionHandling(maxConcurrentCalls, maxAutoRenewDuration);
        }

        /// <summary>
        /// Sets a specific connection for the underlying resource.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="receiveMode"></param>
        /// <param name="retryPolicy"></param>
        public void CustomizeConnection(string connectionString,
            ReceiveMode receiveMode = ReceiveMode.PeekLock,
            RetryPolicy? retryPolicy = null)
        {
            _options.WithConnection(connectionString, receiveMode, retryPolicy);
        }

        /// <summary>
        /// Registers a class as a payload to receive and deserialize through the current resource.
        /// </summary>
        /// <typeparam name="TReceptionModel">The class to deserialize the message into</typeparam>
        /// <typeparam name="THandler">The handler that will receive the deserialized object</typeparam>
        /// <returns></returns>
        public MessageReceptionRegistration RegisterReception<TReceptionModel, THandler>()
            where THandler : class, IMessageReceptionHandler<TReceptionModel>
        {
            _services.TryAddScoped<THandler>();
            var builder = new MessageReceptionRegistration(_options, typeof(TReceptionModel), typeof(THandler));
            _services.Configure<ServiceBusOptions>(options =>
            {
                options.RegisterReception(builder);
            });
            return builder;
        }

        public void EnableSessionHandling(int maxConcurrentSessions = 1, TimeSpan? maxAutoRenewDuration = null)
        {
            _options.EnableSessionHandling(maxConcurrentSessions, maxAutoRenewDuration);
        }
    }
}
