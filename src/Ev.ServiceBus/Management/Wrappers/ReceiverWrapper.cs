﻿using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.Reception;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus;

public class ReceiverWrapper
{
    private readonly ILogger<ReceiverWrapper> _logger;
    private readonly ServiceBusClient? _client;
    private readonly ServiceBusOptions _parentOptions;
    private readonly IServiceProvider _provider;
    private readonly ComposedReceiverOptions _composedOptions;

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
        _logger = _provider.GetRequiredService<ILogger<ReceiverWrapper>>();
    }

    public string ResourceId => _composedOptions.ResourceId;
    public ClientType ClientType => _composedOptions.ClientType;

    internal ServiceBusProcessor? ProcessorClient { get; private set; }
    internal ServiceBusSessionProcessor? SessionProcessorClient { get; private set; }

    public async Task Initialize()
    {
        _logger.LogInformation("[Ev.ServiceBus] Initialization of receiver client '{ResourceId}': Start", ResourceId);
        if (_parentOptions.Settings.Enabled == false)
        {
            _logger.LogInformation(
                "[Ev.ServiceBus] Initialization of client '{ResourceId}': Client deactivated through configuration", ResourceId);
            return;
        }

        await RegisterMessageHandler();
        _logger.LogInformation("[Ev.ServiceBus] Initialization of client '{ResourceId}': Success", ResourceId);
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        if (ProcessorClient != null && ProcessorClient.IsClosed == false)
        {
            try
            {
                await ProcessorClient.CloseAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Ev.ServiceBus] Client {ResourceId} couldn't close properly");
            }
        }

        if (SessionProcessorClient != null && SessionProcessorClient.IsClosed == false)
        {
            try
            {
                await SessionProcessorClient.CloseAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Ev.ServiceBus] Client {ResourceId} couldn't close properly");
            }
        }
    }

    private async Task RegisterMessageHandler()
    {
        if (_parentOptions.Settings.ReceiveMessages == false)
        {
            return;
        }

        if (_composedOptions.SessionMode)
        {
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
        }
        else
        {
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
        }

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
    private async Task OnMessageReceived(MessageContext context)
    {
        using var scope = _provider.CreateScope();
        TrySetReceptionRegistrationOnContext(context, scope);

        var handler = scope.ServiceProvider.GetRequiredService<MessageReceptionHandler>();
        await handler.HandleMessageAsync(context);
    }

    private void TrySetReceptionRegistrationOnContext(MessageContext context, IServiceScope scope)
    {
        if (context.PayloadTypeId == null)
        {
            return;
        }

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
    private async Task OnExceptionOccured(ProcessErrorEventArgs exceptionEvent)
    {
        var json = JsonSerializer.Serialize(new
        {
            exceptionEvent.ErrorSource,
            exceptionEvent.FullyQualifiedNamespace,
            exceptionEvent.EntityPath
        }, new JsonSerializerOptions()
        {
            WriteIndented = true
        });
        _logger.LogError(exceptionEvent.Exception,
            "[Ev.ServiceBus] Exception occured during message treatment of {ClientType} '{ResourceId}'.\n"
            + "Message : {ExceptionMessage}\n"
            + "Context:\n{ContextJson}", _composedOptions.ClientType, ResourceId, exceptionEvent.Exception.Message, json);

        await _onExceptionReceivedHandler!(exceptionEvent);
    }

    private async Task CallDefinedExceptionHandler(ProcessErrorEventArgs exceptionEvent)
    {
        var userDefinedExceptionHandler =
            (IExceptionHandler) _provider.GetService(_composedOptions.ExceptionHandlerType!)!;
        await userDefinedExceptionHandler!.HandleExceptionAsync(exceptionEvent);
    }
}