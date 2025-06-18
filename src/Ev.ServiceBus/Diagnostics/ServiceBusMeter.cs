using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace Ev.ServiceBus.Diagnostics;

public static class ServiceBusMeter
{
    public const string EvServiceBusMessagesSent = "ev.servicebus.messages.sent";
    public const string EvServiceBusMessagesReceived = "ev.servicebus.messages.received";
    public const string EvServiceBusMessagesDeliveryCount = "ev.servicebus.messages.delivery.count";
    public const string EvServiceBusMessageQueueLatency = "ev.servicebus.message.queue.latency";
    
    private static readonly Meter LogMeter = new("Ev.ServiceBus", "1.0.0");

    private static readonly Counter<long> LogMessagesSentCounter = LogMeter.CreateCounter<long>(
        EvServiceBusMessagesSent, "messages", "Total number of messages sent to the service bus.");
    private static readonly Counter<long> LogMessagesReceivedCounter = LogMeter.CreateCounter<long>(
        EvServiceBusMessagesReceived, "messages", "Total number of messages received from the service bus.");

    // Delivery count is the number of deliveries that have been attempted for a single message.
    // The count is incremented when a message lock expires, or the message is explicitly abandoned by the receiver.
    private static readonly Histogram<int> LogMessagesDeliveryCountHistogram = LogMeter.CreateHistogram<int>(
        EvServiceBusMessagesDeliveryCount, "deliveries",
        "Number of deliveries attempted for a single message. Incremented when a message lock expires or the message is explicitly abandoned by the receiver.");

    private static readonly Histogram<double> LogMessageQueueLatencyHistogram = LogMeter.CreateHistogram<double>(
        EvServiceBusMessageQueueLatency, "ms",
        "Time a message spends in the queue from enqueue (by sender) until delivery to the receiver for processing (milliseconds).");

    internal static void IncrementSentCounter(long value, string clientType, string resourceId, string? payloadTypeId)
    {
        LogMessagesSentCounter.Add(value,
            new KeyValuePair<string, object?>("clientType", clientType),
            new KeyValuePair<string, object?>("resourceId", resourceId),
            new KeyValuePair<string, object?>("payloadTypeId", payloadTypeId));
    }

    internal static void IncrementReceivedCounter(long value, string clientType, string resourceId,
        string? payloadTypeId)
    {
        LogMessagesReceivedCounter.Add(value,
            new KeyValuePair<string, object?>("clientType", clientType),
            new KeyValuePair<string, object?>("resourceId", resourceId),
            new KeyValuePair<string, object?>("payloadTypeId", payloadTypeId));
    }

    internal static void RecordDeliveryCount(int value, string clientType, string resourceId, string? payloadTypeId)
    {
        LogMessagesDeliveryCountHistogram.Record(value,
            new KeyValuePair<string, object?>("clientType", clientType),
            new KeyValuePair<string, object?>("resourceId", resourceId),
            new KeyValuePair<string, object?>("payloadTypeId", payloadTypeId));
    }

    internal static void RecordMessageQueueLatency(double value, string clientType, string resourceId,
        string? payloadTypeId)
    {
        LogMessageQueueLatencyHistogram.Record(value,
            new KeyValuePair<string, object?>("clientType", clientType),
            new KeyValuePair<string, object?>("resourceId", resourceId),
            new KeyValuePair<string, object?>("payloadTypeId", payloadTypeId));
    }
}
