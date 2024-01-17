﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using Composer = Ev.ServiceBus.UnitTests.Helpers.Composer;

namespace Ev.ServiceBus.UnitTests;

public class ReceptionConfigurationTest
{
    [Fact]
    public void CustomizePayloadTypeId_ArgumentNullException()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() =>
        {
            services.RegisterServiceBusReception()
                .FromSubscription("topic", "sub",
                    builder =>
                    {
                        builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>()
                            .CustomizePayloadTypeId(null);
                    });
        });
    }

    [Fact]
    public void WrongHandler_ArgumentException()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() =>
        {
            services.RegisterServiceBusReception()
                .FromSubscription("topic", "sub",
                    builder =>
                    {
                        builder.RegisterReception(typeof(SubscribedEvent), typeof(SubscribedEvent));
                    });
        });
    }

    [Fact]
    public void PayloadTypeIdIsAutoGenerated()
    {
        var services = new ServiceCollection();
        services.RegisterServiceBusReception()
            .FromSubscription("topic", "sub",
                builder =>
                {
                    builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
                });
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ServiceBusOptions>>();

        options.Value.ReceptionRegistrations.Count.Should().Be(1);

        var registration = options.Value.ReceptionRegistrations.First();
        registration.PayloadTypeId.Should().Be("SubscribedEvent");
    }

    [Fact]
    public async Task HandlerCannotReceiveFromTheSameSubscriptionTwice()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusReception()
                .FromSubscription("topicName", "subscriptionName",
                    builder =>
                    {
                        builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
                        builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
                    });
        });

        var exception = await Assert.ThrowsAsync<DuplicateSubscriptionHandlerDeclarationException>(async () =>
        {
            await composer.Compose();
        });
        exception.Message.Should().NotBeNull();
        exception.Message.Should().ContainAll("SubscribedEvent", "Subscription", "SubscribedEventHandler");
    }

    [Fact]
    public async Task CanRegisterWithOverload()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusReception().FromQueue("queueName", builder =>
            {
                builder.RegisterReception(typeof(SubscribedEvent), typeof(SubscribedEventHandler));
            });
        });

        await composer.Compose();

        var registry = composer.Provider.GetRequiredService<ServiceBusEngine>();
        var receivers = registry!.GetAllReceivers();
        receivers.Length.Should().Be(1);
        receivers.Should().SatisfyRespectively(
            receiver =>
            {
                receiver.ResourceId.Should().Be("queueName");
            });
    }

    [Fact]
    public async Task CanRegisterFromTheSameQueueTwice_Case1()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusReception().FromQueue("queueName", builder =>
            {
                builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
            });
            services.RegisterServiceBusReception().FromQueue("queueName", builder =>
            {
                builder.RegisterReception<NoiseEvent, NoiseHandler>();
            });
        });

        await composer.Compose();

        var registry = composer.Provider.GetRequiredService<ServiceBusEngine>();
        var receivers = registry!.GetAllReceivers();
        receivers.Length.Should().Be(1);
        receivers.Should().SatisfyRespectively(
            receiver =>
            {
                receiver.ResourceId.Should().Be("queueName");
            });
    }

    [Fact]
    public async Task CanRegisterFromTheSameQueueTwice_Case2()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusReception().FromQueue("queueName", builder =>
            {
                builder.CustomizeConnection("Endpoint=anotherConnectionString;", new ServiceBusClientOptions());

                builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
            });
            services.RegisterServiceBusReception().FromQueue("queueName", builder =>
            {
                builder.RegisterReception<NoiseEvent, NoiseHandler>();
            });
        });

        await composer.Compose();

        var registry = composer.Provider.GetRequiredService<ServiceBusEngine>();
        var receivers = registry!.GetAllReceivers();
        receivers.Length.Should().Be(2);
        receivers.Should().SatisfyRespectively(
            receiver =>
            {
                receiver.ResourceId.Should().Be("queueName");
            },
            receiver =>
            {
                receiver.ResourceId.Should().Be("queueName_2");
            });
    }

    [Fact]
    public async Task CanRegisterFromTheSameSubscriptionTwice_Case1()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusReception().FromSubscription("topicName", "subscriptionName", builder =>
            {
                builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
            });
            services.RegisterServiceBusReception().FromSubscription("topicName", "subscriptionName", builder =>
            {
                builder.RegisterReception<NoiseEvent, NoiseHandler>();
            });
        });

        await composer.Compose();

        var registry = composer.Provider.GetRequiredService<ServiceBusEngine>();
        var receivers = registry!.GetAllReceivers();
        receivers.Length.Should().Be(1);
        receivers.Should().SatisfyRespectively(
            receiver =>
            {
                receiver.ResourceId.Should().Be("topicName/Subscriptions/subscriptionName");
            });
    }

    [Fact]
    public async Task CanRegisterFromTheSameSubscriptionTwice_Case2()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusReception().FromSubscription("topicName", "subscriptionName", builder =>
            {
                builder.CustomizeConnection("Endpoint=anotherConnectionString;", new ServiceBusClientOptions());

                builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
            });
            services.RegisterServiceBusReception().FromSubscription("topicName", "subscriptionName", builder =>
            {
                builder.RegisterReception<NoiseEvent, NoiseHandler>();
            });
        });

        await composer.Compose();

        var registry = composer.Provider.GetRequiredService<ServiceBusEngine>();
        var receivers = registry!.GetAllReceivers();
        receivers.Length.Should().Be(2);
        receivers.Should().SatisfyRespectively(
            receiver =>
            {
                receiver.ResourceId.Should().Be("topicName/Subscriptions/subscriptionName");
            },
            receiver =>
            {
                receiver.ResourceId.Should().Be("topicName/Subscriptions/subscriptionName_2");
            });
    }

    [Fact]
    public async Task HandlerCannotReceiveFromTheSameQueueTwice()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusReception()
                .FromQueue("queueName",
                    builder =>
                    {
                        builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
                        builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
                    });
        });

        var exception = await Assert.ThrowsAsync<DuplicateSubscriptionHandlerDeclarationException>(async () =>
        {
            await composer.Compose();
        });
        exception.Message.Should().NotBeNull();
        exception.Message.Should().ContainAll("Queue", "SubscribedEvent", "SubscribedEventHandler");
    }

    [Fact]
    public async Task PayloadTypeIdCannotBeSetTwice()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusReception()
                .FromQueue("queueName",
                    builder =>
                    {
                        builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>()
                            .CustomizePayloadTypeId("testEvent");
                        builder.RegisterReception<SubscribedEvent, SubscribedEventHandler2>()
                            .CustomizePayloadTypeId("testEvent");
                    });
        });

        var exception = await Assert.ThrowsAsync<DuplicateEvenTypeIdDeclarationException>(async () =>
        {
            await composer.Compose();
        });
        exception.Message.Should().NotBeNull();
        exception.Message.Should().ContainAll("Queue", "SubscribedEventHandler", "SubscribedEventHandler2", "testEvent");
    }

    [Fact]
    public void ReceiveFromQueue_ArgumentCannotBeNull()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(_ => {});

        Assert.Throws<ArgumentNullException>(() =>
        {
            services.RegisterServiceBusReception().FromQueue(null, builder => { });
        });
    }

    [Theory]
    [InlineData(null, "subscriptionName")]
    [InlineData("topicName", null)]
    public void ReceiveFromSubscription_ArgumentCannotBeNull(string topicName, string subscriptionName)
    {
        var services = new ServiceCollection();

        services.AddServiceBus(_ => {});

        Assert.Throws<ArgumentNullException>(() =>
        {
            services.RegisterServiceBusReception()
                .FromSubscription(topicName, subscriptionName, builder => { });
        });
    }

    [Fact]
    public async Task CustomizeMessageHandling_ChangesAreAppliedToClient()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusReception()
                .FromSubscription("testTopic", "testSubscription",
                    builder =>
                    {
                        builder.CustomizeMessageHandling(options =>
                        {
                            options.MaxConcurrentCalls = 3;
                            options.MaxAutoLockRenewalDuration = TimeSpan.FromDays(3);
                            options.ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete;
                        });
                        builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>()
                            .CustomizePayloadTypeId("MyEvent");
                    });
        });

        await composer.Compose();

        var client = composer.ClientFactory.GetProcessorMock("testTopic", "testSubscription");

        client.Options.MaxConcurrentCalls.Should().Be(3);
        client.Options.MaxAutoLockRenewalDuration.Should().Be(TimeSpan.FromDays(3));
        client.Options.ReceiveMode.Should().Be(ServiceBusReceiveMode.ReceiveAndDelete);
    }

    [Fact]
    public async Task CustomizeConnection_ChangesAreAppliedToClient()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusReception()
                .FromSubscription("testTopic", "testSubscription",
                    builder =>
                    {
                        builder.CustomizeConnection("Endpoint=newConnectionString;", new ServiceBusClientOptions()
                        {
                            EnableCrossEntityTransactions = true,
                            TransportType = ServiceBusTransportType.AmqpWebSockets
                        });
                        builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>()
                            .CustomizePayloadTypeId("MyEvent");
                    });
        });

        await composer.Compose();
        var client = composer.ClientFactory.GetAssociatedMock("newConnectionString");
        client.ConnectionSettings.Options.EnableCrossEntityTransactions.Should().Be(true);
        client.ConnectionSettings.Options.TransportType.Should().Be(ServiceBusTransportType.AmqpWebSockets);
    }

    public class SubscribedEventHandler2 : IMessageReceptionHandler<SubscribedEvent>
    {
        public Task Handle(SubscribedEvent @event, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}