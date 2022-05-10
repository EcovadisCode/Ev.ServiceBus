using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public class UnavailableSenderTest
{
    private async Task<IMessageSender> ComposeServiceBusAndGetSender()
    {
        var composer = new Composer();

        composer.OverrideClientFactory(new FailingClientFactory());
        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusQueue("testQueue")
                .WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
        });

        var provider = await composer.Compose();

        return provider.GetRequiredService<IServiceBusRegistry>().GetQueueSender("testQueue");
    }

    [Fact]
    public async Task HaveProperIdentifyingValues()
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
                var message = new ServiceBusMessage();
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
                var message = new ServiceBusMessage();
                await sender.SendMessageAsync(message);
            });
    }

    [Fact]
    public async Task CallsSendAsyncList()
    {
        var sender = await ComposeServiceBusAndGetSender();

        await Assert.ThrowsAsync<MessageSenderUnavailableException>(
            async () =>
            {
                var messageList = new List<ServiceBusMessage>();
                await sender.SendMessagesAsync(messageList);
            });
    }
}