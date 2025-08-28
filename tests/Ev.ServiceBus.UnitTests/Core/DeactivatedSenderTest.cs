using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ev.ServiceBus.UnitTests.Core;

public class DeactivatedSenderTest
{
    private async Task<IMessageSender> ComposeServiceBusAndGetSender()
    {
        var composer = new Composer();

        composer.WithDefaultSettings(
            settings =>
            {
                settings.Enabled = false;
            });
        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch().ToQueue("testQueue", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
                builder.RegisterDispatch<NoiseEvent>();
            });
        });

        var provider = await composer.Compose();

        provider.GetSenderMock("testQueue").Should().BeNull();
        return provider.GetRequiredService<ServiceBusRegistry>().GetMessageSender(ClientType.Queue, "testQueue");
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

        var task = sender.CancelScheduledMessageAsync(16548);

        task.Should().NotBeNull();
        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CallsScheduleMessageAsync()
    {
        var sender = await ComposeServiceBusAndGetSender();

        var message = new ServiceBusMessage();
        var scheduleEnqueueTimeUtc = new DateTimeOffset();
        var task = sender.ScheduleMessageAsync(message, scheduleEnqueueTimeUtc);

        task.Should().NotBeNull();
        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CallsSendAsync()
    {
        var sender = await ComposeServiceBusAndGetSender();

        var message = new ServiceBusMessage();
        var task = sender.SendMessageAsync(message);

        task.Should().NotBeNull();
        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CallsSendAsyncList()
    {
        var sender = await ComposeServiceBusAndGetSender();

        var messageList = new List<ServiceBusMessage>();
        var task = sender.SendMessagesAsync(messageList);

        task.Should().NotBeNull();
        task.IsCompleted.Should().BeTrue();
    }
}