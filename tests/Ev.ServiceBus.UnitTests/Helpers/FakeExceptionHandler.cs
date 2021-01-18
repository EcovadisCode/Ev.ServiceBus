using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Moq;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public class FakeExceptionHandler : IExceptionHandler
    {
        public FakeExceptionHandler()
        {
            Mock = new Mock<IExceptionHandler>();
            Mock.Setup(o => o.HandleExceptionAsync(
                    It.IsAny<ExceptionReceivedEventArgs>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
        }

        public Mock<IExceptionHandler> Mock { get; }

        public Task HandleExceptionAsync(ExceptionReceivedEventArgs args)
        {
            return Mock.Object.HandleExceptionAsync(args);
        }
    }
}
