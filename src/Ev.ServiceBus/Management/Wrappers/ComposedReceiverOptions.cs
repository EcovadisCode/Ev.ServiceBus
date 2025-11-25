using System;
using System.Linq;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Configuration;

namespace Ev.ServiceBus;

public class ComposedReceiverOptions
{
    public ComposedReceiverOptions(ReceiverOptions[] allOptions)
    {
        AllOptions = allOptions;
        ResourceId = allOptions.First().ResourceId;
        ClientType = allOptions.First().ClientType;
        ExceptionHandlerType = allOptions.FirstOrDefault(o => o.ExceptionHandlerType != null)?.ExceptionHandlerType;
        SessionMode = false;
        ConnectionSettings = allOptions.First().ConnectionSettings;
        FirstOption = allOptions.First();

        ProcessorOptions = new ServiceBusProcessorOptions();
        foreach (var config in AllOptions.Select(o => o.ServiceBusProcessorOptions))
        {
            config?.Invoke(ProcessorOptions);
        }

        if (AllOptions.Any(o=> o.SessionProcessorOptions != null))
        {
            SessionMode = true;
            SessionProcessorOptions = new ServiceBusSessionProcessorOptions();
            foreach (var config in AllOptions.Select(o => o.SessionProcessorOptions))
            {
                config?.Invoke(SessionProcessorOptions);
            }
        }
    }

    public ReceiverOptions[] AllOptions { get; }

    public ReceiverOptions FirstOption { get; }
    public bool SessionMode { get; }
    public string ResourceId { get; private set; }
    public ClientType ClientType { get; }
    public Type? ExceptionHandlerType { get; }
    public ServiceBusProcessorOptions ProcessorOptions { get; }
    public ServiceBusSessionProcessorOptions? SessionProcessorOptions { get; }
    public ConnectionSettings? ConnectionSettings { get; }

    internal void UpdateResourceId(string resourceId)
    {
        ResourceId = resourceId;
        foreach (var receiver in AllOptions)
        {
            receiver.UpdateResourceId(resourceId);
        }
    }
}
