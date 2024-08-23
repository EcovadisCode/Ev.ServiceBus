using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Listeners;
using Ev.ServiceBus.Exceptions;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.Reception;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus;

public class ReceiverWrapper
{
    private readonly ILogger<LoggingExtensions.ServiceBusClientManagement> _serviceBusClientManagementLogger;
    private readonly ILogger<LoggingExtensions.MessageProcessing> _messageProcessingLogger;
    private readonly ServiceBusClient? _client;
    private readonly ServiceBusOptions _parentOptions;
    private readonly IServiceProvider _provider;
    private readonly ComposedReceiverOptions _composedOptions;
    private readonly ITransactionManager _transactionManager;

    private Func<ProcessErrorEventArgs, Task>? _onExceptionReceivedHandler;

    public ReceiverWrapper(ServiceBusClient? client,
        ComposedReceiverOptions options,
        ServiceBusOptions parentOptions,
        IServiceProvider provider)
    {
        _client = client;
        _composedOptions = options;
        _parentOptions = parentOptions;
        _provider = provider;
        _transactionManager = _provider.GetRequiredService<ITransactionManager>();
        _serviceBusClientManagementLogger = _provider.GetRequiredService<ILogger<LoggingExtensions.ServiceBusClientManagement>>();
        _messageProcessingLogger = _provider.GetRequiredService<ILogger<LoggingExtensions.MessageProcessing>>();
    }

    internal string ResourceId => _composedOptions.ResourceId;
    private ServiceBusProcessor? ProcessorClient { get; set; }

    public async Task Initialize()
    {
        if (_parentOptions.Settings.Enabled == false)
        {
            _serviceBusClientManagementLogger.ReceiverClientDeactivatedThroughConfiguration(_composedOptions.ResourceId);
            return;
        }

        await RegisterMessageHandler();
        _serviceBusClientManagementLogger.ReceiverClientInitialized(ResourceId);
    }

    public virtual async Task CloseAsync(CancellationToken cancellationToken)
    {
        if (ProcessorClient is { IsClosed: false })
        {
            try
            {
                await ProcessorClient.CloseAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _serviceBusClientManagementLogger.ReceiverClientFailedToClose(_composedOptions.ResourceId, ex);
            }
        }
    }

    protected virtual async Task RegisterMessageHandler()
    {
        if (_parentOptions.Settings.ReceiveMessages == false)
        {
            return;
        }

        ProcessorClient = _composedOptions.FirstOption switch
        {
            QueueOptions queueOptions => _client!.CreateProcessor(queueOptions.QueueName, _composedOptions.ProcessorOptions),
            SubscriptionOptions subscriptionOptions => _client!.CreateProcessor(
                subscriptionOptions.TopicName,
                subscriptionOptions.SubscriptionName,
                _composedOptions.ProcessorOptions),
            _ => ProcessorClient
        };
        ProcessorClient!.ProcessErrorAsync += OnExceptionOccured;
        ProcessorClient.ProcessMessageAsync += args => OnMessageReceived(new MessageContext(args, _composedOptions.ClientType, _composedOptions.ResourceId));
        await ProcessorClient.StartProcessingAsync();

        _onExceptionReceivedHandler = _ => Task.CompletedTask;

        if (_composedOptions.ExceptionHandlerType != null)
        {
            _onExceptionReceivedHandler = CallDefinedExceptionHandler;
        }
    }

    /// <summary>
    ///     Called when a message is received.
    ///     Will create a scope & call the message handler associated with this <see cref="ReceiverWrapper" />.
    /// </summary>
    /// <returns></returns>
    protected async Task OnMessageReceived(MessageContext context)
    {
        using var scope = _provider.CreateScope();
        TrySetReceptionRegistrationOnContext(context, scope);

        var handler = scope.ServiceProvider.GetRequiredService<MessageReceptionHandler>();
        await _transactionManager
            .RunWithInTransaction(
                context.ReadExecutionContext(),
                () => handler.HandleMessageAsync(context));
    }

    private void TrySetReceptionRegistrationOnContext(MessageContext context, IServiceScope scope)
    {
        if (context.PayloadTypeId == null)
            return;

        var registry = scope.ServiceProvider.GetRequiredService<ServiceBusRegistry>();
        var receptionRegistration = registry.GetReceptionRegistration(context.PayloadTypeId,
            context.ResourceId,
            context.ClientType);
        context.ReceptionRegistration = receptionRegistration;
    }

    /// <summary>
    ///     Called whenever an exception occurs during the handling of a message.
    /// </summary>
    /// <param name="exceptionEvent"></param>
    /// <returns></returns>
    protected async Task OnExceptionOccured(ProcessErrorEventArgs exceptionEvent)
    {
        var processException = exceptionEvent.Exception as FailedToProcessMessageException;
        using (_messageProcessingLogger.ProcessingInProgress(
                   clientType: processException?.ClientType ?? _composedOptions.ClientType.ToString(),
                   resourceId: processException?.ResourceId ?? _composedOptions.ResourceId,
                   messageId: processException?.MessageId,
                   payloadTypeId: processException?.PayloadTypeId,
                   sessionId: processException?.SessionId,
                   handlerName: processException?.HandlerName))
        {
            var processExceptionInnerException = processException is not null ? processException.InnerException! : exceptionEvent.Exception!;
            _messageProcessingLogger.FailedToProcessMessage(
                exceptionEvent.ErrorSource.ToString(),
                exceptionEvent.FullyQualifiedNamespace,
                exceptionEvent.EntityPath,
                processExceptionInnerException);

            await _onExceptionReceivedHandler!(exceptionEvent);
        }
    }

    protected async Task CallDefinedExceptionHandler(ProcessErrorEventArgs exceptionEvent)
    {
        var userDefinedExceptionHandler =
            (IExceptionHandler) _provider.GetService(_composedOptions.ExceptionHandlerType!)!;
        await userDefinedExceptionHandler!.HandleExceptionAsync(exceptionEvent);
    }
}