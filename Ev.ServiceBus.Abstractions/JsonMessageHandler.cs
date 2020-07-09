using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ev.ServiceBus.Abstractions
{
    /// <summary>
    /// A helper abstract class for parsing json content from the message.
    /// </summary>
    /// <typeparam name="TBody">To what type should the json string be converted.</typeparam>
    public abstract class JsonMessageHandler<TBody> : IMessageHandler
       where TBody : class
    {
        public Task HandleMessageAsync(MessageContext context)
        {
            var json = Encoding.UTF8.GetString(context.Message.Body);
            var body = JsonConvert.DeserializeObject<TBody>(json);
            return HandleMessageAsync(context, body);
        }
        protected abstract Task HandleMessageAsync(MessageContext context, TBody body);
    }
}
