using System;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Moq;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public class FakeMessageHandler : IMessageHandler
    {
        private readonly Func<MessageContext, Task> _action;
        public Mock<IMessageHandler> Mock { get; set; }

        public FakeMessageHandler()
        {
            Mock = new Mock<IMessageHandler>();
            Mock.Setup(o => o.HandleMessageAsync(It.IsAny<MessageContext>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
        }

        public FakeMessageHandler(Func<MessageContext, Task> action) : this()
        {
            _action = action;
        }

        public async Task HandleMessageAsync(MessageContext context)
        {
            if (_action != null)
            {
                await _action(context);
            }

            await Mock.Object.HandleMessageAsync(context);
        }
    }
}
