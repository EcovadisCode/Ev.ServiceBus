using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions;

/// <summary>
///     Exposes all the methods necessary to the treatment completion of an incoming message.
/// </summary>
public interface IMessageReceiver : IClient
{
    /// <summary>
    ///     Completes a <see cref="T:Microsoft.Azure.ServiceBus.Message" /> using its lock token. This will delete the message
    ///     from the topic.
    /// </summary>
    /// <param name="lockToken">The lock token of the corresponding message to complete.</param>
    /// <remarks>
    ///     A lock token can be found in
    ///     <see cref="P:Microsoft.Azure.ServiceBus.Message.SystemPropertiesCollection.LockToken" />,
    ///     only when <see cref="P:Microsoft.Azure.ServiceBus.Core.IReceiverClient.ReceiveMode" /> is set to
    ///     <see cref="F:Microsoft.Azure.ServiceBus.ReceiveMode.PeekLock" />.
    ///     This operation can only be performed on messages that were received by this receiver.
    /// </remarks>
    Task CompleteAsync(string lockToken);

    /// <summary>
    ///     Abandons a <see cref="T:Microsoft.Azure.ServiceBus.Message" /> using a lock token. This will make the message
    ///     available again for processing.
    /// </summary>
    /// <param name="lockToken">The lock token of the corresponding message to abandon.</param>
    /// <param name="propertiesToModify">The properties of the message to modify while abandoning the message.</param>
    /// <remarks>
    ///     A lock token can be found in
    ///     <see cref="P:Microsoft.Azure.ServiceBus.Message.SystemPropertiesCollection.LockToken" />,
    ///     only when <see cref="P:Microsoft.Azure.ServiceBus.Core.IReceiverClient.ReceiveMode" /> is set to
    ///     <see cref="F:Microsoft.Azure.ServiceBus.ReceiveMode.PeekLock" />.
    ///     Abandoning a message will increase the delivery count on the message.
    ///     This operation can only be performed on messages that were received by this receiver.
    /// </remarks>
    Task AbandonAsync(string lockToken, IDictionary<string, object>? propertiesToModify = null);

    /// <summary>Moves a message to the deadletter sub-topic.</summary>
    /// <param name="lockToken">The lock token of the corresponding message to deadletter.</param>
    /// <param name="propertiesToModify">The properties of the message to modify while moving to sub-topic.</param>
    /// <remarks>
    ///     A lock token can be found in
    ///     <see cref="P:Microsoft.Azure.ServiceBus.Message.SystemPropertiesCollection.LockToken" />,
    ///     only when <see cref="P:Microsoft.Azure.ServiceBus.Core.IReceiverClient.ReceiveMode" /> is set to
    ///     <see cref="F:Microsoft.Azure.ServiceBus.ReceiveMode.PeekLock" />.
    ///     In order to receive a message from the deadletter topic, you will need a new
    ///     <see cref="T:Microsoft.Azure.ServiceBus.Core.IMessageReceiver" />, with the corresponding path.
    ///     You can use <see cref="M:Microsoft.Azure.ServiceBus.EntityNameHelper.FormatDeadLetterPath(System.String)" /> to
    ///     help with this.
    ///     This operation can only be performed on messages that were received by this receiver.
    /// </remarks>
    Task DeadLetterAsync(string lockToken, IDictionary<string, object>? propertiesToModify = null);

    /// <summary>Moves a message to the deadletter sub-topic.</summary>
    /// <param name="lockToken">The lock token of the corresponding message to deadletter.</param>
    /// <param name="deadLetterReason">The reason for deadlettering the message.</param>
    /// <param name="deadLetterErrorDescription">The error description for deadlettering the message.</param>
    /// <remarks>
    ///     A lock token can be found in
    ///     <see cref="P:Microsoft.Azure.ServiceBus.Message.SystemPropertiesCollection.LockToken" />,
    ///     only when <see cref="P:Microsoft.Azure.ServiceBus.Core.IReceiverClient.ReceiveMode" /> is set to
    ///     <see cref="F:Microsoft.Azure.ServiceBus.ReceiveMode.PeekLock" />.
    ///     In order to receive a message from the deadletter topic, you will need a new
    ///     <see cref="T:Microsoft.Azure.ServiceBus.Core.IMessageReceiver" />, with the corresponding path.
    ///     You can use <see cref="M:Microsoft.Azure.ServiceBus.EntityNameHelper.FormatDeadLetterPath(System.String)" /> to
    ///     help with this.
    ///     This operation can only be performed on messages that were received by this receiver.
    /// </remarks>
    Task DeadLetterAsync(string lockToken, string deadLetterReason, string? deadLetterErrorDescription = null);
}