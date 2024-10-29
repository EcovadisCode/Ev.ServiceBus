using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus;

public static class LoggingExtensions
{
    #region ServiceBusClientManagement

    public record ServiceBusClientManagement();

    private static readonly Action<ILogger, string, Exception?> LogReceiverClientInitialized =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2, nameof(ReceiverClientInitialized)),
            "Reception client for {EVSB_ResourceId} initialized"
        );

    public static void ReceiverClientInitialized(this ILogger logger, string resourceId)
        => LogReceiverClientInitialized(logger, resourceId, default);

    private static readonly Action<ILogger, string, Exception?> LogSenderClientInitialized =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, nameof(SenderClientInitialized)),
            "Sender client for {EVSB_ResourceId} initialized"
        );

    public static void SenderClientInitialized(this ILogger logger, string resourceId)
        => LogSenderClientInitialized(logger, resourceId, default);

    private static readonly Action<ILogger, string, Exception> LogReceiverClientFailedToClose =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2, nameof(ReceiverClientFailedToClose)),
            "Reception client for {EVSB_ResourceId} failed to close"
        );

    public static void ReceiverClientFailedToClose(this ILogger logger, string resourceId, Exception exception)
        => LogReceiverClientFailedToClose(logger, resourceId, exception);

    private static readonly Action<ILogger, string, Exception> LogSenderClientFailedToClose =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, nameof(SenderClientFailedToClose)),
            "Sender client for {EVSB_ResourceId} failed to close"
        );

    public static void SenderClientFailedToClose(this ILogger logger, string resourceId, Exception exception)
        => LogSenderClientFailedToClose(logger, resourceId, exception);

    private static readonly Action<ILogger, string, Exception> LogReceiverClientFailedToInitialize =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(4, nameof(ReceiverClientFailedToInitialize)),
            "Receiver client for {EVSB_ResourceId} failed to close"
        );

    public static void ReceiverClientFailedToInitialize(this ILogger logger, string resourceId, Exception exception)
        => LogReceiverClientFailedToInitialize(logger, resourceId, exception);

    private static readonly Action<ILogger, string, Exception> LogSenderClientFailedToInitialize =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(5, nameof(SenderClientFailedToInitialize)),
            "Sender client for {EVSB_ResourceId} failed to close"
        );

    public static void SenderClientFailedToInitialize(this ILogger logger, string resourceId, Exception exception)
        => LogSenderClientFailedToInitialize(logger, resourceId, exception);

    private static readonly Action<ILogger, string, Exception?> LogReceiverClientDeactivatedThroughConfiguration =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(6, nameof(ReceiverClientDeactivatedThroughConfiguration)),
            "Initialization of receiver client '{ResourceId}': Client deactivated through configuration"
        );

    public static void ReceiverClientDeactivatedThroughConfiguration(this ILogger logger, string resourceId)
        => LogReceiverClientDeactivatedThroughConfiguration(logger, resourceId, default);

    private static readonly Action<ILogger, string, Exception?> LogSenderClientDeactivatedThroughConfiguration =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(7, nameof(SenderClientDeactivatedThroughConfiguration)),
            "Initialization of sender client '{ResourceId}': Client deactivated through configuration"
        );

    public static void SenderClientDeactivatedThroughConfiguration(this ILogger logger, string resourceId)
        => LogSenderClientDeactivatedThroughConfiguration(logger, resourceId, default);

    #endregion

    #region ServiceBusEngine

    public record ServiceBusEngine();

    private static readonly Action<ILogger, bool, bool, Exception?> LogEngineStarting =
        LoggerMessage.Define<bool, bool>(
            LogLevel.Information,
            new EventId(1, nameof(EngineStarting)),
            "Starting service bus engine with settings enabled {EVSB_ServiceBusEnabled} " +
            "and reception enabled {EVSB_ReceptionEnabled}"
        );

    public static void EngineStarting(this ILogger logger, bool enabled, bool receptionEnabled)
        => LogEngineStarting(logger, enabled, receptionEnabled, default);

    private static readonly Action<ILogger, Exception?> LogEngineStopping = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(2, nameof(EngineStopping)),
        "Stopping service bus engine"
    );

    public static void EngineStopping(this ILogger logger)
        => LogEngineStopping(logger, default);

    private static readonly Action<ILogger, Exception> LogFailedToConnectToServiceBus =
        LoggerMessage.Define(
            LogLevel.Critical,
            new EventId(3, nameof(FailedToConnectToServiceBus)),
            "Failed to connect to service bus"
        );

    public static void FailedToConnectToServiceBus(this ILogger logger, Exception exception)
        => LogFailedToConnectToServiceBus(logger, exception);

    #endregion

    #region MessageProcessing

    public record MessageProcessing();

    public static IDisposable? ProcessingInProgress(
        this ILogger logger,
        string? clientType,
        string? resourceId,
        string? payloadTypeId,
        string? messageId,
        string? sessionId,
        string? handlerName)
        => logger.BeginScope(new Dictionary<string, string?> () {
            ["EVSB_Client"] = clientType,
            ["EVSB_ResourceId"] = resourceId,
            ["EVSB_PayloadTypeId"] = payloadTypeId,
            ["EVSB_MessageId"] = messageId,
            ["EVSB_SessionId"] = sessionId,
            ["EVSB_ReceptionHandler"] = handlerName
        });

    private static readonly Action<ILogger, long, Exception?> LogMessageExecutionCompleted = LoggerMessage.Define<long>(
        LogLevel.Information,
        new EventId(1, nameof(ProcessingInProgress)),
        "Message processing completed in {EVSB_Duration} milliseconds"
    );

    public static void MessageExecutionCompleted(this ILogger logger, long executionDuration)
        => LogMessageExecutionCompleted(logger, executionDuration, default);

    private static readonly Action<ILogger, string, string, string, Exception> LogFailedToProcessMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(3, "MessageProcessing"),
            "Failed to process message Error Source: {EVSB_ErrorSource}, Namespace :{EVSB_Namespace} , EntityPath : {EVSB_EntityPath}"
        );

    public static void FailedToProcessMessage(this ILogger logger, string errorSource, string @namespace,
        string entityPath, Exception exception)
        => LogFailedToProcessMessage(logger, errorSource, @namespace, entityPath, exception);

    #endregion
}