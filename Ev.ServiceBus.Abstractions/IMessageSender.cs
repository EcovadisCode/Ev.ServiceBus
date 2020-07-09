using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace Ev.ServiceBus.Abstractions
{
    /// <summary>
    ///     Exposes all the methods available for sending a message through a queue.
    /// </summary>
    public interface IMessageSender : IClient
    {
        /// <summary>Sends a message to Service Bus.</summary>
        Task SendAsync(Message message);

        /// <summary>Sends a list of messages to Service Bus.</summary>
        Task SendAsync(IList<Message> messageList);

        /// <summary>Schedules a message to appear on Service Bus.</summary>
        /// <param name="message">The message That will be scheduled</param>
        /// <param name="scheduleEnqueueTimeUtc">The UTC time that the message should be available for processing</param>
        /// <returns>The sequence number of the message that was scheduled.</returns>
        Task<long> ScheduleMessageAsync(Message message, DateTimeOffset scheduleEnqueueTimeUtc);

        /// <summary>Cancels a message that was scheduled.</summary>
        /// <param name="sequenceNumber">
        ///     The
        ///     <see cref="P:Microsoft.Azure.ServiceBus.Message.SystemPropertiesCollection.SequenceNumber" /> of the message to be
        ///     cancelled.
        /// </param>
        Task CancelScheduledMessageAsync(long sequenceNumber);
    }
}
