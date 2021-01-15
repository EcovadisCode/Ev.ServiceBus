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
    public class DeactivatedSenderTest
    {
        private async Task<IMessageSender> ComposeServiceBusAndGetSender()
        {
            var composer = new ServiceBusComposer();

            composer.WithDefaultSettings(
                settings =>
                {
                    settings.Enabled = false;
                });
            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusQueue("testQueue")
                    .WithConnection("testConnectionString");
            });

            var provider = await composer.ComposeAndSimulateStartup();

            provider.GetQueueClientMock("testQueue").Should().BeNull();
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

            var task = sender.CancelScheduledMessageAsync(16548);

            task.Should().NotBeNull();
            task.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task CallsScheduleMessageAsync()
        {
            var sender = await ComposeServiceBusAndGetSender();

            var message = new Message();
            var scheduleEnqueueTimeUtc = new DateTimeOffset();
            var task = sender.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc);

            task.Should().NotBeNull();
            task.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task CallsSendAsync()
        {
            var sender = await ComposeServiceBusAndGetSender();

            var message = new Message();
            var task = sender.SendAsync(message);

            task.Should().NotBeNull();
            task.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task CallsSendAsyncList()
        {
            var sender = await ComposeServiceBusAndGetSender();

            var messageList = new List<Message>();
            var task = sender.SendAsync(messageList);

            task.Should().NotBeNull();
            task.IsCompleted.Should().BeTrue();
        }
    }
}
