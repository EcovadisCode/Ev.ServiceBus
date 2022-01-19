using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Ev.ServiceBus
{
    public class ReceiverWrapper
    {
        private readonly ConnectionSettings? _connectionSettings;
        private readonly Type? _exceptionHandlerType;
        private readonly ILogger<ReceiverWrapper> _logger;
        private readonly Type _messageHandlerType;
        private readonly IMessageReceiverOptions[] _options;
        private readonly ServiceBusOptions _parentOptions;
        private readonly IServiceProvider _provider;

        private Func<ExceptionReceivedEventArgs, Task>? _onExceptionReceivedHandler;
        private MessageReceiver? _receiver;

        public ReceiverWrapper(IMessageReceiverOptions[] options,
            ServiceBusOptions parentOptions,
            IServiceProvider provider)
        {
            ResourceId = options.First().ResourceId;
            ClientType = options.First().ClientType;
            _connectionSettings = options.First().ConnectionSettings;
            _messageHandlerType = options.First().MessageHandlerType!;
            _exceptionHandlerType = options.FirstOrDefault(o => o.ExceptionHandlerType != null)?.ExceptionHandlerType;
            _options = options;
            _parentOptions = parentOptions;
            _provider = provider;
            _logger = _provider.GetRequiredService<ILogger<ReceiverWrapper>>();
        }

        public string ResourceId { get; }
        public ClientType ClientType { get; }

        internal IReceiverClient? ReceiverClient { get; private set; }

        public void Initialize()
        {
            _logger.LogInformation("[Ev.ServiceBus] Initialization of receiver client '{ResourceId}': Start", ResourceId);
            if (_parentOptions.Settings.Enabled == false)
            {
                _receiver = null;
                _logger.LogInformation(
                    "[Ev.ServiceBus] Initialization of client '{ResourceId}': Client deactivated through configuration", ResourceId);
                return;
            }

            try
            {
                var connectionSettings = _connectionSettings ?? _parentOptions.Settings.ConnectionSettings;
                if (connectionSettings == null)
                {
                    throw new MissingConnectionException(ResourceId, ClientType.Topic);
                }

                switch (ClientType)
                {
                    case ClientType.Queue:
                        CreateQueueClient(connectionSettings);
                        break;
                    case ClientType.Subscription:
                        CreateSubscriptionClient(connectionSettings);
                        break;
                }

                RegisterMessageHandler(_options, _receiver!);
                _logger.LogInformation("[Ev.ServiceBus] Initialization of client '{ResourceId}': Success", ResourceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Ev.ServiceBus] Initialization of client '{ResourceId}': Failed", ResourceId);
            }
        }

        private void CreateQueueClient(ConnectionSettings settings)
        {
            var factory = _provider.GetRequiredService<IClientFactory<QueueOptions, IQueueClient>>();
            ReceiverClient = factory.Create((QueueOptions) _options.First(), settings);
            _receiver = new MessageReceiver(ReceiverClient, ResourceId, ClientType);
        }

        private void CreateSubscriptionClient(ConnectionSettings settings)
        {
            var factory = _provider.GetRequiredService<IClientFactory<SubscriptionOptions, ISubscriptionClient>>();
            ReceiverClient = factory.Create((SubscriptionOptions) _options.First(), settings);
            _receiver = new MessageReceiver(ReceiverClient, ResourceId, ClientType);
        }

        private void RegisterMessageHandler(IMessageReceiverOptions[] receiverOptions, MessageReceiver receiver)
        {
            if (_parentOptions.Settings.ReceiveMessages == false)
            {
                return;
            }

            _onExceptionReceivedHandler = exceptionEvent => Task.CompletedTask;

            if (_exceptionHandlerType != null)
            {
                _onExceptionReceivedHandler = CallDefinedExceptionHandler;
            }

            if (receiverOptions.Any(o => o.SessionHandlerConfig != null))
            {
                var sessionHandlerOptions = new SessionHandlerOptions(OnExceptionOccured);

                var options = receiverOptions.Where(o => o.SessionHandlerConfig != null).Select(o => o.SessionHandlerConfig).First();
                options!(sessionHandlerOptions);

                switch (ClientType)
                {
                    case ClientType.Queue:
                        ((IQueueClient)receiver.Client).RegisterSessionHandler(async (session, message, cancellationToken) => { await OnMessageReceived(message, cancellationToken); }, sessionHandlerOptions);
                        break;
                    case ClientType.Subscription:
                        ((ISubscriptionClient)receiver.Client).RegisterSessionHandler(async(session, message, cancellationToken) => { await OnMessageReceived(message, cancellationToken); }, sessionHandlerOptions);
                        break;
                }
            }
            else
            {
                var messageHandlerOptions = new MessageHandlerOptions(OnExceptionOccured);

                foreach (var config in receiverOptions.Where(o => o.MessageHandlerConfig != null)
                             .Select(o => o.MessageHandlerConfig))
                {
                    config!(messageHandlerOptions);
                }

                receiver.Client.RegisterMessageHandler(OnMessageReceived, messageHandlerOptions);
            }
        }

        /// <summary>
        ///     Called when a message is received.
        ///     Will create a scope & call the message handler associated with this <see cref="ReceiverWrapper" />.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task OnMessageReceived(Message message, CancellationToken cancellationToken)
        {
            var sw = new Stopwatch();
            var scopeValues = new Dictionary<string, string>
            {
                ["EVSB_Client"] = ClientType.ToString(),
                ["EVSB_ResourceId"] = ResourceId,
                ["EVSB_Handler"] = _messageHandlerType.FullName!,
                ["EVSB_MessageId"] = message.MessageId,
            };
            using (_logger.BeginScope(scopeValues))
            using (var scope = _provider.CreateScope())
            {
                var metadataAccessor = (MessageMetadataAccessor)scope.ServiceProvider.GetRequiredService<IMessageMetadataAccessor>();
                metadataAccessor.SetData(message, cancellationToken);
                var listeners = scope.ServiceProvider.GetRequiredService<IEnumerable<IServiceBusEventListener>>();
                var executionStartedArgs = new ExecutionStartedArgs(ClientType, ResourceId, _messageHandlerType, message);
                foreach (var listener in listeners)
                {
                    await listener.OnExecutionStart(executionStartedArgs);
                }
                _logger.LogInformation("[Ev.ServiceBus] New message received from {EVSB_Client} '{EVSB_ResourceId}' : {EVSB_MessageLabel}", ClientType, ResourceId, message.Label);

                var messageHandler = (IMessageHandler) scope.ServiceProvider.GetRequiredService(_messageHandlerType);
                sw.Start();
                try
                {
                    var context = new MessageContext(message, _receiver!, cancellationToken);
                    await messageHandler.HandleMessageAsync(context).ConfigureAwait(false);
                }
                catch (Exception ex) when (LogError(ex))
                {
                    var executionFailedArgs = new ExecutionFailedArgs(ClientType, ResourceId, _messageHandlerType, message, ex);
                    foreach (var listener in listeners)
                    {
                        await listener.OnExecutionFailed(executionFailedArgs);
                    }
                    throw;
                }
                finally
                {
                    sw.Stop();
                }
                var executionSucceededArgs = new ExecutionSucceededArgs(ClientType, ResourceId, _messageHandlerType, message, sw.ElapsedMilliseconds);
                foreach (var listener in listeners)
                {
                    await listener.OnExecutionSuccess(executionSucceededArgs);
                }
                _logger.LogInformation("[Ev.ServiceBus] Message finished execution in {EVSB_Duration} milliseconds", sw.ElapsedMilliseconds);
            }
        }

        /// <summary>
        ///     workaround to attach the log scope to the logged exception
        ///     https://andrewlock.net/how-to-include-scopes-when-logging-exceptions-in-asp-net-core/
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private bool LogError(Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return true;
        }

        /// <summary>
        ///     Called whenever an exception occurs during the handling of a message.
        /// </summary>
        /// <param name="exceptionEvent"></param>
        /// <returns></returns>
        private async Task OnExceptionOccured(ExceptionReceivedEventArgs exceptionEvent)
        {
            var json = JsonConvert.SerializeObject(exceptionEvent.ExceptionReceivedContext, Formatting.Indented);
            _logger.LogError(exceptionEvent.Exception,
                "[Ev.ServiceBus] Exception occured during message treatment of {ClientType} '{ResourceId}'.\n"
                + "Message : {ExceptionMessage}\n"
                + "Context:\n{ContextJson}", _receiver!.ClientType, ResourceId, exceptionEvent.Exception.Message, json);

            await _onExceptionReceivedHandler!(exceptionEvent).ConfigureAwait(false);
        }

        private async Task CallDefinedExceptionHandler(ExceptionReceivedEventArgs exceptionEvent)
        {
            var userDefinedExceptionHandler =
                (IExceptionHandler) _provider.GetService(_exceptionHandlerType!)!;
            await userDefinedExceptionHandler!.HandleExceptionAsync(exceptionEvent).ConfigureAwait(false);
        }
    }
}
