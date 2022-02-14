using System;
using System.Linq;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;

namespace Ev.ServiceBus;

public class ComposedReceiverOptions
{
    private readonly IMessageReceiverOptions[] _options;

    public ComposedReceiverOptions(IMessageReceiverOptions[] options)
    {
        _options = options;
        ResourceId = options.First().ResourceId;
        ClientType = options.First().ClientType;
        ExceptionHandlerType = options.FirstOrDefault(o => o.ExceptionHandlerType != null)?.ExceptionHandlerType;
        SessionMode = false;
        ConnectionSettings = options.First().ConnectionSettings;
        MessageHandlerType = options.First().MessageHandlerType!;
        FirstOption = options.First();

        ProcessorOptions = new ServiceBusProcessorOptions();
        foreach (var config in _options.Select(o => o.ServiceBusProcessorOptions))
        {
            config?.Invoke(ProcessorOptions);
        }

        if (_options.Any(o=> o.SessionProcessorOptions != null))
        {
            SessionMode = true;
            SessionProcessorOptions = new ServiceBusSessionProcessorOptions();
            foreach (var config in _options.Select(o => o.SessionProcessorOptions))
            {
                config?.Invoke(SessionProcessorOptions);
            }
        }
    }

    public IMessageReceiverOptions FirstOption { get; }
    public Type MessageHandlerType { get; }
    public bool SessionMode { get; }
    public string ResourceId { get; }
    public ClientType ClientType { get; }
    public Type? ExceptionHandlerType { get; }
    public ServiceBusProcessorOptions ProcessorOptions { get; }
    public ServiceBusSessionProcessorOptions? SessionProcessorOptions { get; }
    public ConnectionSettings? ConnectionSettings { get; }
}
