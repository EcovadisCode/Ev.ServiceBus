using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
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
        internal MessageReceiver? Receiver;

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
            _logger.LogInformation($"[Ev.ServiceBus] Initialization of client '{ResourceId}': Start.");
            if (_parentOptions.Settings.Enabled == false)
            {
                Receiver = null;
                _logger.LogInformation(
                    $"[Ev.ServiceBus] Initialization of client '{ResourceId}': Client deactivated through configuration.");
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

                RegisterMessageHandler(_options, Receiver!);
                _logger.LogInformation($"[Ev.ServiceBus] Initialization of client '{ResourceId}': Success.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Ev.ServiceBus] Initialization of client '{ResourceId}': Failed.");
            }
        }

        private void CreateQueueClient(ConnectionSettings settings)
        {
            var factory = _provider.GetRequiredService<IClientFactory<QueueOptions, IQueueClient>>();
            ReceiverClient = factory.Create((QueueOptions) _options.First(), settings);
            Receiver = new MessageReceiver(ReceiverClient, ResourceId, ClientType);
        }

        private void CreateSubscriptionClient(ConnectionSettings settings)
        {
            var factory = _provider.GetRequiredService<IClientFactory<SubscriptionOptions, ISubscriptionClient>>();
            ReceiverClient = factory.Create((SubscriptionOptions) _options.First(), settings);
            Receiver = new MessageReceiver(ReceiverClient, ResourceId, ClientType);
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

            var messageHandlerOptions = new MessageHandlerOptions(OnExceptionOccured);

            foreach (var config in receiverOptions.Where(o => o.MessageHandlerConfig != null)
                .Select(o => o.MessageHandlerConfig))
            {
                config!(messageHandlerOptions);
            }

            receiver.Client.RegisterMessageHandler(OnMessageReceived, messageHandlerOptions);
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
            _logger.LogInformation(
                $"[Ev.ServiceBus] New message received from {Receiver!.ClientType} '{Receiver.Name}' : {message.Label}");

            using var traceLogger = new MsgTraceLogger(_logger,
                $"[Ev.ServiceBus] Message from {Receiver.ClientType}: {Receiver.Name}: {message.MessageId}.");
            using var scope = _provider.CreateScope();
            var messageHandler =
                (IMessageHandler) scope.ServiceProvider.GetRequiredService(_messageHandlerType);
            var context = new MessageContext(message,
                Receiver,
                cancellationToken);
            await messageHandler.HandleMessageAsync(context).ConfigureAwait(false);
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
                $"[Ev.ServiceBus] Exception occured during message treatment of {Receiver!.ClientType} '{Receiver.Name}'.\n"
                + $"Message : {exceptionEvent.Exception.Message}\n"
                + $"Context:\n{json}");

            await _onExceptionReceivedHandler!(exceptionEvent).ConfigureAwait(false);
        }

        private async Task CallDefinedExceptionHandler(ExceptionReceivedEventArgs exceptionEvent)
        {
            var userDefinedExceptionHandler =
                (IExceptionHandler) _provider.GetService(_exceptionHandlerType!)!;
            await userDefinedExceptionHandler!.HandleExceptionAsync(exceptionEvent).ConfigureAwait(false);
        }
    }

    internal class MsgTraceLogger : IDisposable
    {
        private readonly ILogger<ReceiverWrapper> _logger;
        private readonly string _msg;
        private readonly Stopwatch _sw;

        public MsgTraceLogger(ILogger<ReceiverWrapper> logger, string msg)
        {
            _logger = logger;
            _msg = msg;
            _sw = new Stopwatch();
            _sw.Start();
        }

        public void Dispose()
        {
            var executionTime = _sw.ElapsedMilliseconds;
            _sw.Stop();
            _logger.LogInformation($"{_msg}: executed in: {executionTime} milliseconds.");
        }
    }
}
