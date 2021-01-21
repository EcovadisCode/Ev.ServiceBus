using System;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public interface IMessageReceiverOptions : IClientOptions
    {
        Type? MessageHandlerType { get; }
        Action<MessageHandlerOptions>? MessageHandlerConfig { get; }
        Type? ExceptionHandlerType { get; }
    }
}