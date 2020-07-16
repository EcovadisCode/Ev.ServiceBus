using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Ev.ServiceBus
{
    public abstract class ReceiverWrapper : BaseWrapper
    {
        private readonly ILogger<ReceiverWrapper> _logger;
        private readonly IMessageReceiverOptions _receiverOptions;

        private Func<ExceptionReceivedEventArgs, Task> _onExceptionReceivedHandler;

        protected ReceiverWrapper(
            IMessageReceiverOptions receiverOptions,
            ServiceBusOptions parentOptions,
            IServiceProvider provider,
            string name)
            : base(parentOptions, provider, name)
        {
            _receiverOptions = receiverOptions;
            _logger = Provider.GetRequiredService<ILogger<ReceiverWrapper>>();
        }

        protected void RegisterMessageHandler(IMessageReceiverOptions receiverOptions, MessageReceiver receiver)
        {
            if (ParentOptions.ReceiveMessages == false)
            {
                return;
            }
            if (receiverOptions.MessageHandlerType == null)
            {
                return;
            }

            _onExceptionReceivedHandler = (exceptionEvent) => Task.CompletedTask;

            if (receiverOptions.ExceptionHandlerType != null)
            {
                _onExceptionReceivedHandler = CallDefinedExceptionHandler;
            }

            var messageHandlerOptions = new MessageHandlerOptions(OnExceptionOccured);

            receiverOptions.MessageHandlerConfig?.Invoke(messageHandlerOptions);

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
                $"New message received from {Receiver.ClientType} '{Receiver.Name}' : {message.Label}");

            using var traceLogger = new MsgTraceLogger(_logger, $"Message from {Receiver.ClientType}: {Receiver.Name}: {message.MessageId}.");
            using var scope = Provider.CreateScope();
            var messageHandler =
                (IMessageHandler) scope.ServiceProvider.GetRequiredService(_receiverOptions.MessageHandlerType);
            var context = new MessageContext(
                message,
                Receiver,
                cancellationToken);
            await messageHandler.HandleMessageAsync(context);
        }

        /// <summary>
        ///     Called whenever an exception occurs during the handling of a message.
        /// </summary>
        /// <param name="exceptionEvent"></param>
        /// <returns></returns>
        private async Task OnExceptionOccured(ExceptionReceivedEventArgs exceptionEvent)
        {
            var json = JsonConvert.SerializeObject(exceptionEvent.ExceptionReceivedContext, Formatting.Indented);
            _logger.LogError(
                exceptionEvent.Exception,
                $"Exception occured during message treatment of {Receiver.ClientType} '{Receiver.Name}'.\n"
                + $"Message : {exceptionEvent.Exception.Message}\n"
                + $"Context:\n{json}");

            await _onExceptionReceivedHandler(exceptionEvent);
        }

        private async Task CallDefinedExceptionHandler(ExceptionReceivedEventArgs exceptionEvent)
        {
            var userDefinedExceptionHandler =
                (IExceptionHandler) Provider.GetService(_receiverOptions.ExceptionHandlerType);
            await userDefinedExceptionHandler.HandleExceptionAsync(exceptionEvent);
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