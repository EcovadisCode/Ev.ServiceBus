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
        private async Task<QueueClientMock> RegisterHandlerAndComposeServiceBus(Func<MessageContext, Task> callback)
        {
            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(services =>
            {
                services.AddSingleton(new FakeMessageHandler(callback));

                services.ConfigureServiceBus(options =>
                {
                    options.RegisterQueue("testQueue")
                        .WithConnectionString("testConnectionString")
                       .WithCustomMessageHandler<FakeMessageHandler>();
                });
            });

            var provider = await composer.ComposeAndSimulateStartup();

            return provider.GetQueueClientMock("testQueue");
        }

        [Fact]
        public async Task CallsAbandonAsync()
        {
            var clientMock = await RegisterHandlerAndComposeServiceBus(MessageHandler);

            await clientMock.TriggerMessageReception(message: new Message(), token: new CancellationToken());

            async Task MessageHandler(MessageContext context)
            {
                await context.Receiver.AbandonAsync("lockTokenTest");
            }

            clientMock.Mock.Verify(o => o.AbandonAsync("lockTokenTest", null), Times.Once);
        }

        [Fact]
        public async Task CallsCompleteAsync()
        {
            var clientMock = await RegisterHandlerAndComposeServiceBus(MessageHandler);

            await clientMock.TriggerMessageReception(message: new Message(), token: new CancellationToken());

            async Task MessageHandler(MessageContext context)
            {
                await context.Receiver.CompleteAsync("lockTokenTest");
            }

            clientMock.Mock.Verify(o => o.CompleteAsync("lockTokenTest"), Times.Once);
        }

        [Fact]
        public async Task CallsDeadLetterAsync()
        {
            var clientMock = await RegisterHandlerAndComposeServiceBus(MessageHandler);

            await clientMock.TriggerMessageReception(message: new Message(), token: new CancellationToken());

            async Task MessageHandler(MessageContext context)
            {
                await context.Receiver.DeadLetterAsync("lockTokenTest");
            }

            clientMock.Mock.Verify(o => o.DeadLetterAsync("lockTokenTest", null), Times.Once);
        }

        [Fact]
        public async Task CallsDeadLetterAsyncWithReason()
        {
            var clientMock = await RegisterHandlerAndComposeServiceBus(MessageHandler);

            await clientMock.TriggerMessageReception(message: new Message(), token: new CancellationToken());

            async Task MessageHandler(MessageContext context)
            {
                await context.Receiver.DeadLetterAsync("lockTokenTest", "testReason", "testDescription");
            }

            clientMock.Mock.Verify(o => o.DeadLetterAsync("lockTokenTest", "testReason", "testDescription"), Times.Once);
        }
    }
}
