using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.TestHelpers;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class SenderTest
    {
        private async Task<(IMessageSender sender, SenderMock clientMock)> ComposeServiceBusAndGetSender()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusQueue("testQueue")
                    .WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
            });

            var provider = await composer.Compose();

            return (
                provider.GetRequiredService<IServiceBusRegistry>().GetQueueSender("testQueue"),
                provider.GetSenderMock("testQueue")
            );
        }

        [Fact]
        public async Task HaveProperIdentifyingValues()
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

            clientMock.Mock.Verify(o => o.CancelScheduledMessageAsync(16548, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CallsScheduleMessageAsync()
        {
            var (sender, clientMock) = await ComposeServiceBusAndGetSender();

            var message = new ServiceBusMessage();
            var scheduleEnqueueTimeUtc = new DateTimeOffset();
            await sender.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc);

            clientMock.Mock.Verify(o => o.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CallsSendAsync()
        {
            var (sender, clientMock) = await ComposeServiceBusAndGetSender();

            var message = new ServiceBusMessage();
            await sender.SendMessageAsync(message);

            clientMock.Mock.Verify(o => o.SendMessageAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CallsSendAsyncList()
        {
            var (sender, clientMock) = await ComposeServiceBusAndGetSender();

            var messageList = new List<ServiceBusMessage>();
            await sender.SendMessagesAsync(messageList);

            clientMock.Mock.Verify(o => o.SendMessagesAsync(messageList, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
