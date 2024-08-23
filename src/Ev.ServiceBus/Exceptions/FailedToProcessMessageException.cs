using System;

namespace Ev.ServiceBus.Exceptions;

public class FailedToProcessMessageException(
    string? clientType,
    string? resourceId,
    string? messageId,
    string? payloadTypeId,
    string? sessionId,
    string? handlerName,
    Exception innerException)
    : Exception("Failed to process Message", innerException)
{
    public string? ClientType { get; } = clientType;
    public string? ResourceId { get; } = resourceId;
    public string? MessageId { get; } = messageId;
    public string? PayloadTypeId { get; } = payloadTypeId;
    public string? SessionId { get; } = sessionId;
    public string? HandlerName { get; } = handlerName;
}