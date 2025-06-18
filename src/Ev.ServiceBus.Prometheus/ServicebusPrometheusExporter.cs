using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Diagnostics;
using Microsoft.Extensions.Hosting;
using Prometheus;

namespace Ev.ServiceBus.Prometheus;

public class ServicebusPrometheusExporter : IHostedService, IDisposable
{
    private static readonly HashSet<string> _instrumentations =
    [
        ServiceBusMeter.EvServiceBusMessagesSent,
        ServiceBusMeter.EvServiceBusMessagesReceived,
        ServiceBusMeter.EvServiceBusMessagesDeliveryCount,
        ServiceBusMeter.EvServiceBusMessageQueueLatency
    ];
    private MeterListener _listener;

    // Only these tags are used as labels
    private static readonly string[] LabelNames = ["clientType", "resourceId", "payloadTypeId"];

    // Prometheus metrics
    private readonly Dictionary<string, Counter> _counters = new();
    private readonly Dictionary<string, Histogram> _histograms = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _listener = new MeterListener();
        CreateCounter(ServiceBusMeter.EvServiceBusMessagesSent, LabelNames);
        CreateCounter(ServiceBusMeter.EvServiceBusMessagesReceived, LabelNames);
        CreateHistogram(ServiceBusMeter.EvServiceBusMessagesDeliveryCount, LabelNames);
        CreateHistogram(ServiceBusMeter.EvServiceBusMessageQueueLatency, LabelNames);

        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Ev.ServiceBus" &&
                _instrumentations.Contains(instrument.Name))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            var (labels, labelValues) = ExtractLabels(tags);
            var counter = _counters[instrument.Name];
            counter.WithLabels(labelValues).Inc(value);
        });
        _listener.SetMeasurementEventCallback<int>((instrument, value, tags, state) =>
        {
            var (labels, labelValues) = ExtractLabels(tags);
            var histogram = _histograms[instrument.Name];
            histogram.WithLabels(labelValues).Observe(value);
        });
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, state) =>
        {
            var (labels, labelValues) = ExtractLabels(tags);
            var histogram = _histograms[instrument.Name];
            histogram.WithLabels(labelValues).Observe(value);
        });
        _listener.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _listener?.Dispose();
        return Task.CompletedTask;
    }

    private static (string[] labels, string?[] labelValues) ExtractLabels(
        ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var values = new string[LabelNames.Length];
        for (var i = 0; i < LabelNames.Length; i++)
        {
            values[i] = "";
            foreach (var tag in tags)
            {
                if (tag.Key != LabelNames[i] || tag.Value is null)
                    continue;

                values[i] = tag.Value?.ToString();
                break;
            }
        }

        return (LabelNames, values);
    }

    private void CreateCounter(string name, string[] labels)
    {
        var counter = Metrics.CreateCounter(name.Replace('.', '_'), $"Counter for {name}", labels);
        _counters[name] = counter;
    }

    private void CreateHistogram(string name, string[] labels)
    {
        var histogram = Metrics.CreateHistogram(name.Replace('.', '_'), $"Histogram for {name}", labels);
        _histograms[name] = histogram;
    }

    public void Dispose()
    {
        _listener?.Dispose();
    }
}