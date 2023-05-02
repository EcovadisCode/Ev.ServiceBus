using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Reception;
using Moq;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public class FakeMessageHandler : IMessageHandler
    {
        public Mock<IMessageHandler> Mock { get; set; }

        public FakeMessageHandler(Mock<IMessageHandler> mock)
        {
            Mock = mock;
        }

        public async Task HandleMessageAsync(MessageContext context)
        {
            await Mock.Object.HandleMessageAsync(context);
        }
    }
}
