using System;
using Azure.Messaging.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public interface IMessageReceiverOptions : IClientOptions
    {
        /// <summary>
        /// Type of a class inheriting from <see cref="IMessageHandler"/> that will be resolved whenever a message is executed.
        /// </summary>
        Type? MessageHandlerType { get; }

        /// <summary>
        /// Settings specific to this reception handler
        /// </summary>
        Action<ServiceBusProcessorOptions>? ServiceBusProcessorOptions { get; }

        /// <summary>
        /// Type of a class inheriting from <see cref="IExceptionHandler"/> that will be resolved
        /// whenever an unhandled exception is thrown during message execution.
        /// (Only system exception will end up here)
        /// </summary>
        Type? ExceptionHandlerType { get; }

        /// <summary>
        /// Settings specific to this reception handler
        /// </summary>
        Action<ServiceBusSessionProcessorOptions>? SessionProcessorOptions { get; }
    }
}