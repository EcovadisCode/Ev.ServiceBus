using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public interface IMessageHandler
    {
        Task HandleMessageAsync(MessageContext context);
    }
}
