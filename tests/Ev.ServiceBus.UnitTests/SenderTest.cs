using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class SenderTest
    {
        private async Task<(IMessageSender sender, QueueClientMock clientMock)> ComposeServiceBusAndGetSender()
        {
            var composer = new ServiceBusComposer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusQueue("testQueue")
                    .WithConnection("testConnectionString");
            });

            var provider = await composer.ComposeAndSimulateStartup();

            return (
                provider.GetRequiredService<IServiceBusRegistry>().GetQueueSender("testQueue"),
                provider.GetQueueClientMock("testQueue", false)
            );
        }

        [Fact]
        public async Task HaveProperidentifyingValues()
        {
            var (sender, clientMock) = await ComposeServiceBusAndGetSender();

            sender.Name.Should().Be("testQueue");
            sender.ClientType.Should().Be(ClientType.Queue);
        }

        [Fact]
        public async Task CallsCancelScheduledMessageAsync()
        {
            var (sender, clientMock) = await ComposeServiceBusAndGetSender();

            await sender.CancelScheduledMessageAsync(16548);

            clientMock.Mock.Verify(o => o.CancelScheduledMessageAsync(16548), Times.Once);
        }

        [Fact]
        public async Task CallsScheduleMessageAsync()
        {
            var (sender, clientMock) = await ComposeServiceBusAndGetSender();

            var message = new Message();
            var scheduleEnqueueTimeUtc = new DateTimeOffset();
            await sender.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc);

            clientMock.Mock.Verify(o => o.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc), Times.Once);
        }

        [Fact]
        public async Task CallsSendAsync()
        {
            var (sender, clientMock) = await ComposeServiceBusAndGetSender();

            var message = new Message();
            await sender.SendAsync(message);

            clientMock.Mock.Verify(o => o.SendAsync(message), Times.Once);
        }

        [Fact]
        public async Task CallsSendAsyncList()
        {
            var (sender, clientMock) = await ComposeServiceBusAndGetSender();

            var messageList = new List<Message>();
            await sender.SendAsync(messageList);

            clientMock.Mock.Verify(o => o.SendAsync(messageList), Times.Once);
        }
    }
}
