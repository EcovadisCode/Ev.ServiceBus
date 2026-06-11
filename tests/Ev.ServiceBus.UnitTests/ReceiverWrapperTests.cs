using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Listeners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public sealed class ReceiverWrapperTests
{
    private static TestableReceiverWrapper CreateWrapper(
        ILogger<LoggingExtensions.MessageProcessing>? messageLogger = null,
        ITransactionManager? transactionManager = null)
    {
        var mockServices = new Mock<IServiceCollection>();
        var composedOptions = new ComposedReceiverOptions([new QueueOptions(mockServices.Object, "test-queue")]);
        var parentOptions = new ServiceBusOptions();

        var mockProvider = new Mock<IServiceProvider>();
        mockProvider.Setup(p => p.GetService(typeof(ITransactionManager)))
            .Returns(transactionManager ?? Mock.Of<ITransactionManager>());
        mockProvider.Setup(p => p.GetService(typeof(ILogger<LoggingExtensions.ServiceBusClientManagement>)))
            .Returns(Mock.Of<ILogger<LoggingExtensions.ServiceBusClientManagement>>());
        mockProvider.Setup(p => p.GetService(typeof(ILogger<LoggingExtensions.MessageProcessing>)))
            .Returns(messageLogger ?? Mock.Of<ILogger<LoggingExtensions.MessageProcessing>>());

        return new TestableReceiverWrapper(composedOptions, parentOptions, mockProvider.Object);
    }

    [Fact]
    public async Task OnExceptionOccured_WithOperationCanceledException_DoesNotLogError()
    {
        var mockLogger = new Mock<ILogger<LoggingExtensions.MessageProcessing>>();
        var wrapper = CreateWrapper(mockLogger.Object);

        // Azure SDK raises ProcessErrorAsync with CancellationToken.None during shutdown —
        // the token on the exception is not the shutdown token, so IsCancellationRequested is false.
        var args = new ProcessErrorEventArgs(
            new OperationCanceledException("shutdown", CancellationToken.None),
            ServiceBusErrorSource.Receive,
            "test-namespace",
            "test-queue",
            CancellationToken.None);

        await wrapper.InvokeOnExceptionOccuredAsync(args);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Never());
    }

    [Fact]
    public async Task OnExceptionOccured_WithTaskCanceledException_DoesNotLogError()
    {
        var mockLogger = new Mock<ILogger<LoggingExtensions.MessageProcessing>>();
        var wrapper = CreateWrapper(mockLogger.Object);

        var args = new ProcessErrorEventArgs(
            new TaskCanceledException(),
            ServiceBusErrorSource.Receive,
            "test-namespace",
            "test-queue",
            CancellationToken.None);

        await wrapper.InvokeOnExceptionOccuredAsync(args);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Never());
    }

    [Fact]
    public async Task OnExceptionOccured_WithOperationCanceledException_CallsOnReceiveCancelled()
    {
        var mockTransactionManager = new Mock<ITransactionManager>();
        var cancellationAware = mockTransactionManager.As<ICancellationAwareTransactionManager>();
        var wrapper = CreateWrapper(transactionManager: mockTransactionManager.Object);

        var args = new ProcessErrorEventArgs(
            new OperationCanceledException("shutdown", CancellationToken.None),
            ServiceBusErrorSource.Receive,
            "test-namespace",
            "test-queue",
            CancellationToken.None);

        await wrapper.InvokeOnExceptionOccuredAsync(args);

        cancellationAware.Verify(x => x.OnReceiveCancelled(), Times.Once());
    }

    [Fact]
    public async Task OnExceptionOccured_WithNonCancelledException_DoesNotCallOnReceiveCancelled()
    {
        var mockTransactionManager = new Mock<ITransactionManager>();
        var cancellationAware = mockTransactionManager.As<ICancellationAwareTransactionManager>();
        var wrapper = CreateWrapper(transactionManager: mockTransactionManager.Object);

        var args = new ProcessErrorEventArgs(
            new InvalidOperationException("connection lost"),
            ServiceBusErrorSource.Receive,
            "test-namespace",
            "test-queue",
            CancellationToken.None);

        await wrapper.InvokeOnExceptionOccuredAsync(args);

        cancellationAware.Verify(x => x.OnReceiveCancelled(), Times.Never());
    }

    [Fact]
    public async Task OnExceptionOccured_WithNonCancelledToken_LogsError()
    {
        var mockLogger = new Mock<ILogger<LoggingExtensions.MessageProcessing>>();
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Error)).Returns(true);
        var wrapper = CreateWrapper(mockLogger.Object);

        var args = new ProcessErrorEventArgs(
            new InvalidOperationException("connection lost"),
            ServiceBusErrorSource.Receive,
            "test-namespace",
            "test-queue",
            CancellationToken.None);

        await wrapper.InvokeOnExceptionOccuredAsync(args);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once());
    }

    private sealed class TestableReceiverWrapper : ReceiverWrapper
    {
        public TestableReceiverWrapper(
            ComposedReceiverOptions options,
            ServiceBusOptions parentOptions,
            IServiceProvider provider)
            : base(null, options, parentOptions, provider) { }

        public Task InvokeOnExceptionOccuredAsync(ProcessErrorEventArgs args) => OnExceptionOccured(args);
    }
}
