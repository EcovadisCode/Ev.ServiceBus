using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class UnavailableSenderTest
    {
        private async Task<IMessageSender> ComposeServiceBusAndGetSender()
        {
            var composer = new ServiceBusComposer();

            composer.OverrideQueueClientFactory(new FailingClientFactory());
            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusQueue("testQueue")
                    .WithConnection("testConnectionString");
            });

            var provider = await composer.ComposeAndSimulateStartup();

            return provider.GetRequiredService<IServiceBusRegistry>().GetQueueSender("testQueue");
        }

        [Fact]
        public async Task HaveProperidentifyingValues()
        {
            var sender = await ComposeServiceBusAndGetSender();

            sender.Name.Should().Be("testQueue");
            sender.ClientType.Should().Be(ClientType.Queue);
        }

        [Fact]
        public async Task CallsCancelScheduledMessageAsync()
        {
            var sender = await ComposeServiceBusAndGetSender();

            await Assert.ThrowsAsync<MessageSenderUnavailableException>(
                async () =>
                {
                    await sender.CancelScheduledMessageAsync(16548);
                });
        }

        [Fact]
        public async Task CallsScheduleMessageAsync()
        {
            var sender = await ComposeServiceBusAndGetSender();

            await Assert.ThrowsAsync<MessageSenderUnavailableException>(
                async () =>
                {
                    var message = new Message();
                    var scheduleEnqueueTimeUtc = new DateTimeOffset();
                    await sender.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc);
                });
        }

        [Fact]
        public async Task CallsSendAsync()
        {
            var sender = await ComposeServiceBusAndGetSender();

            await Assert.ThrowsAsync<MessageSenderUnavailableException>(
                async () =>
                {
                    var message = new Message();
                    await sender.SendAsync(message);
                });
        }

        [Fact]
        public async Task CallsSendAsyncList()
        {
            var sender = await ComposeServiceBusAndGetSender();

            await Assert.ThrowsAsync<MessageSenderUnavailableException>(
                async () =>
                {
                    var messageList = new List<Message>();
                    await sender.SendAsync(messageList);
                });
        }
    }
}
