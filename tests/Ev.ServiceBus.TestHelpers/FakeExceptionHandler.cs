using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Moq;

namespace Ev.ServiceBus.UnitTests.Helpers;

public class FakeExceptionHandler : IExceptionHandler
{
    private readonly Mock<IExceptionHandler> _mock;

    public FakeExceptionHandler(Mock<IExceptionHandler> mock)
    {
        _mock = mock;
    }

    public Task HandleExceptionAsync(ProcessErrorEventArgs args)
    {
        return _mock.Object.HandleExceptionAsync(args);
    }
}