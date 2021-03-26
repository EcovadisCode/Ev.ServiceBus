using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    /// <summary>
    /// Base interface for a message handler. Use this if you don't want to use the message reception feature.
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Called when a message is received from the linked ServiceBus resource.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task HandleMessageAsync(MessageContext context);
    }
}
