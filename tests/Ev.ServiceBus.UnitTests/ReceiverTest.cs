using System;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class ReceiverTest
    {
        private async Task<(QueueClientMock clientMock, Mock<IMessageHandler> messageHandlerMock)> RegisterHandlerAndComposeServiceBus(Action<MessageContext> callback)
        {
            var composer = new ServiceBusComposer();
            var mock = new Mock<IMessageHandler>();
            mock.Setup(o => o.HandleMessageAsync(It.IsAny<MessageContext>()))
                .Returns(Task.CompletedTask)
                .Callback(callback)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.AddSingleton(mock);

                    services.RegisterServiceBusQueue("testQueue")
                        .WithConnection("testConnectionString")
                        .WithCustomMessageHandler<FakeMessageHandler>();
                });

            var provider = await composer.ComposeAndSimulateStartup();

            return (provider.GetQueueClientMock("testQueue"), mock);
        }

        [Fact]
        public async Task CallsAbandonAsync()
        {
            var mocks = await RegisterHandlerAndComposeServiceBus(MessageHandler);

            await mocks.clientMock.TriggerMessageReception(new Message(), new CancellationToken());

            void MessageHandler(MessageContext context)
            {
                context.Receiver.AbandonAsync("lockTokenTest").ConfigureAwait(false).GetAwaiter().GetResult();
            }

            mocks.clientMock.Mock.Verify(o => o.AbandonAsync("lockTokenTest", null), Times.Once);
            mocks.messageHandlerMock.VerifyAll();
        }

        [Fact]
        public async Task CallsCompleteAsync()
        {
            var mocks = await RegisterHandlerAndComposeServiceBus(MessageHandler);

            await mocks.clientMock.TriggerMessageReception(new Message(), new CancellationToken());

            void MessageHandler(MessageContext context)
            {
                context.Receiver.CompleteAsync("lockTokenTest").ConfigureAwait(false).GetAwaiter().GetResult();
            }

            mocks.clientMock.Mock.Verify(o => o.CompleteAsync("lockTokenTest"), Times.Once);
            mocks.messageHandlerMock.VerifyAll();
        }

        [Fact]
        public async Task CallsDeadLetterAsync()
        {
            var mocks = await RegisterHandlerAndComposeServiceBus(MessageHandler);

            await mocks.clientMock.TriggerMessageReception(new Message(), new CancellationToken());

            void MessageHandler(MessageContext context)
            {
                context.Receiver.DeadLetterAsync("lockTokenTest").ConfigureAwait(false).GetAwaiter().GetResult();
            }

            mocks.clientMock.Mock.Verify(o => o.DeadLetterAsync("lockTokenTest", null), Times.Once);
            mocks.messageHandlerMock.VerifyAll();
        }

        [Fact]
        public async Task CallsDeadLetterAsyncWithReason()
        {
            var mocks = await RegisterHandlerAndComposeServiceBus(MessageHandler);

            await mocks.clientMock.TriggerMessageReception(new Message(), new CancellationToken());

            void MessageHandler(MessageContext context)
            {
                context.Receiver.DeadLetterAsync("lockTokenTest", "testReason", "testDescription").ConfigureAwait(false).GetAwaiter().GetResult();
            }

            mocks.clientMock.Mock.Verify(
                o => o.DeadLetterAsync("lockTokenTest", "testReason", "testDescription"),
                Times.Once);
            mocks.messageHandlerMock.VerifyAll();
        }
    }
}
