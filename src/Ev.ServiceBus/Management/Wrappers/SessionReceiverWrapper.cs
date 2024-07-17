using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus;

public class SessionReceiverWrapper : ReceiverWrapper
{
    private readonly ILogger<LoggingExtensions.ServiceBusClientManagement> _logger;
    private readonly ServiceBusClient? _client;
    private readonly ServiceBusOptions _parentOptions;
    private readonly IServiceProvider _provider;
    private readonly ComposedReceiverOptions _composedOptions;

    private Func<ProcessErrorEventArgs, Task>? _onExceptionReceivedHandler;

    public SessionReceiverWrapper(ServiceBusClient? client,
        ComposedReceiverOptions options,
        ServiceBusOptions parentOptions,
        IServiceProvider provider)
        : base(client, options, parentOptions, provider)
    {
        _client = client;
        _composedOptions = options;
        _parentOptions = parentOptions;
        _provider = provider;
        _logger = _provider.GetRequiredService<ILogger<LoggingExtensions.ServiceBusClientManagement>>();
    }

    private ServiceBusSessionProcessor? SessionProcessorClient { get; set; }

    public override async Task CloseAsync(CancellationToken cancellationToken)
    {
        if (SessionProcessorClient != null && SessionProcessorClient.IsClosed == false)
        {
            try
            {
                await SessionProcessorClient.CloseAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.ReceiverClientFailedToClose(_composedOptions.ResourceId, ex);
            }
        }
    }

    protected override async Task RegisterMessageHandler()
    {
        if (_parentOptions.Settings.ReceiveMessages == false)
        {
            return;
        }

        SessionProcessorClient = _composedOptions.FirstOption switch
        {
            QueueOptions queueOptions => _client!.CreateSessionProcessor(queueOptions.QueueName, _composedOptions.SessionProcessorOptions),
            SubscriptionOptions subscriptionOptions => _client!.CreateSessionProcessor(
                subscriptionOptions.TopicName,
                subscriptionOptions.SubscriptionName,
                _composedOptions.SessionProcessorOptions),
            _ => SessionProcessorClient
        };
        SessionProcessorClient!.ProcessErrorAsync += OnExceptionOccured;
        SessionProcessorClient.ProcessMessageAsync += args => OnMessageReceived(new MessageContext(args, _composedOptions.ClientType, _composedOptions.ResourceId));
        await SessionProcessorClient.StartProcessingAsync();

        _onExceptionReceivedHandler = _ => Task.CompletedTask;

        if (_composedOptions.ExceptionHandlerType != null)
        {
            _onExceptionReceivedHandler = CallDefinedExceptionHandler;
        }
    }

}
