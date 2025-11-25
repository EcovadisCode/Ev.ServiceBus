using System;
using Azure.Messaging.ServiceBus;
using System.Collections.Generic;

namespace Ev.ServiceBus.Abstractions;

public static class MessageHelper
{
    public static string? GetPayloadTypeId(this ServiceBusReceivedMessage message)
    {
        return TryGetValue(message, UserProperties.PayloadTypeIdProperty);
    }

    public static TimeSpan GetQueueLatency(this ServiceBusReceivedMessage message)
    {
        var enqueuedTime = message.ScheduledEnqueueTime == default ? message.EnqueuedTime : message.ScheduledEnqueueTime;
        return DateTimeOffset.UtcNow - enqueuedTime;
    }

    private static string? TryGetValue(ServiceBusReceivedMessage message, string propertyName)
    {
        message.ApplicationProperties.TryGetValue(propertyName, out var value);
        return value as string;
    }

    public static ServiceBusMessage CreateMessage(string contentType, byte[] body, string payloadTypeId)
    {
        var message = new ServiceBusMessage(body)
        {
            ContentType = contentType,
            Subject = $"An Ev.ServiceBus message of type '{payloadTypeId}'",
            ApplicationProperties =
            {
                {UserProperties.MessageTypeProperty, "IntegrationEvent"},
                {UserProperties.PayloadTypeIdProperty, payloadTypeId}
            }
        };
        return message;
    }

    public static string[] GetIsolationApps(this ServiceBusReceivedMessage message)
    {
        var appsString = TryGetValue(message, UserProperties.IsolationApps);
        if (string.IsNullOrEmpty(appsString))
            return [];
        return appsString.Split(',');
    }

    public static string[] GetIsolationApps(this IReadOnlyDictionary<string, object> applicationProperties)
    {
        if (applicationProperties == null)
            return [];
        applicationProperties.TryGetValue(UserProperties.IsolationApps, out var value);
        return value == null ? [] : ((string)value).Split(',');
    }

    public static string? GetIsolationKey(this ServiceBusReceivedMessage message)
    {
        return TryGetValue(message, UserProperties.IsolationKey);
    }

    public static string? GetIsolationKey(this IReadOnlyDictionary<string, object> applicationProperties)
    {
        if (applicationProperties == null) return null;
        applicationProperties.TryGetValue(UserProperties.IsolationKey, out var value);
        return value == null ? null : (string)value;
    }

    public static string? GetIsolationKey(this IDictionary<string, object> applicationProperties)
    {
        applicationProperties.TryGetValue(UserProperties.IsolationKey, out var value);
        return value == null ? null : (string)value;
    }

    public static ServiceBusMessage SetIsolationKey(this ServiceBusMessage message, string? isolationKey)
    {
        if (string.IsNullOrEmpty(isolationKey))
            return message;
        message.ApplicationProperties[UserProperties.IsolationKey] = isolationKey;
        return message;
    }

    public static ServiceBusMessage SetIsolationApps(this ServiceBusMessage message, string[] isolationApps)
    {
        if (isolationApps.Length == 0)
            return message;
        message.ApplicationProperties[UserProperties.IsolationApps] = string.Join(',', isolationApps);
        return message;
    }

    public static void SetIsolationKey(this IDictionary<string, object> applicationProperties, string? isolationKey)
    {
        if (string.IsNullOrEmpty(isolationKey))
            return;
        applicationProperties[UserProperties.IsolationKey] = isolationKey;
    }
}