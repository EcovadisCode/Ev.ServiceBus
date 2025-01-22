using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ev.ServiceBus.Abstractions;

public interface IDispatchSender
{
    /// <summary>
    /// Immediately serializes object to message and sends it through ServiceBus.
    /// Single message on Premium tier can be bigger than 1MB.
    /// </summary>
    /// <param name="messagePayload"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task SendDispatch(object messagePayload, CancellationToken token = default);

    /// <summary>
    /// Immediately serializes object to message and sends it through ServiceBus.
    /// Single message on Premium tier can be bigger than 1MB.
    /// </summary>
    /// <param name="messagePayload"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task SendDispatch(Dispatch messagePayload, CancellationToken token = default);

    /// <summary>
    /// Immediately serializes object to messages and sends them through ServiceBus.
    /// (This is used internally by <see cref="IMessageDispatcher.ExecuteDispatches"/>)
    /// </summary>
    /// <param name="messagePayloads"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task SendDispatches(IEnumerable<object> messagePayloads, CancellationToken token = default);

    /// <summary>
    /// Immediately serializes dispatches to messages and sends them through ServiceBus.
    /// (This is used internally by <see cref="IMessageDispatcher.ExecuteDispatches"/>)
    /// </summary>
    /// <param name="messagePayloads"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task SendDispatches(IEnumerable<Dispatch> messagePayloads, CancellationToken token = default);

    /// <summary>
    /// Immediately serializes an object into a message and sends it through the registered ServiceBus resources as a scheduled message.
    /// </summary>
    /// <param name="messagePayloads"></param>
    /// <param name="scheduledEnqueueTime"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task ScheduleDispatches(IEnumerable<object> messagePayloads, DateTimeOffset scheduledEnqueueTime, CancellationToken token = default);

    /// <summary>
    /// Immediately serializes a <see cref="Dispatch"/> into a message and sends it through the registered ServiceBus resources as a scheduled message.
    /// </summary>
    /// <param name="messagePayloads"></param>
    /// <param name="scheduledEnqueueTime"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task ScheduleDispatches(IEnumerable<Dispatch> messagePayloads, DateTimeOffset scheduledEnqueueTime, CancellationToken token = default);
}