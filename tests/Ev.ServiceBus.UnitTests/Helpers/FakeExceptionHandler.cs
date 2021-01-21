using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Microsoft.Azure.ServiceBus;
using Moq;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public class FakeExceptionHandler : IExceptionHandler
    {
        private readonly Mock<IExceptionHandler> _mock;

        public FakeExceptionHandler(Mock<IExceptionHandler> mock)
        {
            _mock = mock;
        }

        public Task HandleExceptionAsync(ExceptionReceivedEventArgs args)
        {
            return _mock.Object.HandleExceptionAsync(args);
        }
    }
}
