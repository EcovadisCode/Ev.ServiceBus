﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Composer = Ev.ServiceBus.UnitTests.Helpers.Composer;

namespace Ev.ServiceBus.UnitTests;

public class DispatchConfigurationTest
{
    [Fact]
    public void CustomizePayloadTypeId_ArgumentNullException()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(
            () =>
            {
                services.RegisterServiceBusDispatch().ToTopic("topicName", builder =>
                {
                    builder.RegisterDispatch<PublishedEvent>().CustomizePayloadTypeId(null);
                });
            });
    }

    [Fact]
    public void PayloadTypeIdIsAutoGenerated()
    {
        var services = new ServiceCollection();
        services.RegisterServiceBusDispatch().ToTopic("topicName", builder =>
        {
            var reg = builder.RegisterDispatch<PublishedEvent>();
            reg.PayloadTypeId.Should().Be("PublishedEvent");
        });
    }

    [Fact]
    public async Task ThrowsIfYouDispatchAnUnregisteredMessage()
    {
        var composer = new Composer();

        await composer.Compose();

        var sender = composer.Provider.GetRequiredService<IDispatchSender>();
        await Assert.ThrowsAsync<DispatchRegistrationNotFoundException>(async () =>
        {
            await sender.SendDispatches(new[] { new PublishedEvent() });
        });

        await Assert.ThrowsAsync<DispatchRegistrationNotFoundException>(async () =>
        {
            await sender.SendDispatches(new[] { new Abstractions.Dispatch(new PublishedEvent()) });
        });
    }

    [Fact]
    public async Task ThrowsIfYouDispatchAnUnregisteredMessage_case2()
    {
        var composer = new Composer();

        await composer.Compose();

        var publisher = composer.Provider.GetRequiredService<IMessagePublisher>();
        var dispatcher = composer.Provider.GetRequiredService<IMessageDispatcher>();

        publisher.Publish(new PublishedEvent());
        await Assert.ThrowsAsync<DispatchRegistrationNotFoundException>(async () =>
        {
            await dispatcher.ExecuteDispatches(CancellationToken.None);
        });
    }

    [Fact]
    public async Task CanRegisterSameTopicSenderTwice_Case1()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch().ToTopic("topicName", builder =>
            {
                builder.RegisterDispatch<PublishedEvent>();
            });
            services.RegisterServiceBusDispatch().ToTopic("topicName", builder =>
            {
                builder.RegisterDispatch<NoiseEvent>();
            });
        });

        await composer.Compose();

        var registry = composer.Provider.GetRequiredService<ServiceBusRegistry>();
        var senders = registry!.GetAllSenderClients();
        senders.Length.Should().Be(1);
        senders.First().EntityPath.Should().Be("topicName");
    }

    [Fact]
    public async Task CanRegisterSameTopicSenderTwice_Case2()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch().ToTopic("topicName", builder =>
            {
                builder.CustomizeConnection("Endpoint=anotherConnectionString;", new ServiceBusClientOptions());
                builder.RegisterDispatch<PublishedEvent>();
            });
            services.RegisterServiceBusDispatch().ToTopic("topicName", builder =>
            {
                builder.RegisterDispatch<NoiseEvent>();
            });
        });

        await composer.Compose();

        var registry = composer.Provider.GetRequiredService<ServiceBusRegistry>();
        var senders = registry!.GetAllSenderClients();
        senders.Length.Should().Be(2);
        senders.Should().SatisfyRespectively(
            sender =>
            {
                sender.EntityPath.Should().Be("topicName");
            },
            sender =>
            {
                sender.EntityPath.Should().Be("topicName_2");
            });
    }

    [Fact]
    public async Task RegisteringAQueueDispatchWontRegisterAReceiver()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.RegisterDispatch<PublishedEvent>();
            });
        });

        await composer.Compose();

        var registry = composer.Provider.GetRequiredService<ServiceBusRegistry>();
        var senders = registry!.GetAllSenderClients();
        senders.Length.Should().Be(1);
        senders.First().EntityPath.Should().Be("queue");
        var receiver = registry!.GetAllReceivers();
        receiver.Length.Should().Be(0);
    }

    [Fact]
    public async Task CanRegisterSameQueueSenderTwice()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.RegisterDispatch<PublishedEvent>();
            });
            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.RegisterDispatch<NoiseEvent>();
            });
        });

        await composer.Compose();

        var registry = composer.Provider.GetRequiredService<ServiceBusRegistry>();
        var senders = registry!.GetAllSenderClients();
        senders.Length.Should().Be(1);
        senders.First().EntityPath.Should().Be("queue");
    }

    [Fact]
    public async Task CanRegisterSameQueueSenderTwice_Case2()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch().ToQueue("queueName", builder =>
            {
                builder.CustomizeConnection("Endpoint=anotherConnectionString;", new ServiceBusClientOptions());
                builder.RegisterDispatch<PublishedEvent>();
            });
            services.RegisterServiceBusDispatch().ToQueue("queueName", builder =>
            {
                builder.RegisterDispatch<NoiseEvent>();
            });
        });

        await composer.Compose();

        var registry = composer.Provider.GetRequiredService<ServiceBusRegistry>();
        var senders = registry!.GetAllSenderClients();
        senders.Length.Should().Be(2);
        senders.Should().SatisfyRespectively(
            sender =>
            {
                sender.EntityPath.Should().Be("queueName");
            },
            sender =>
            {
                sender.EntityPath.Should().Be("queueName_2");
            });
    }

    [Fact]
    public async Task CannotRegisterTheSameContractMoreThanOnce()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch().ToQueue("queueName", builder =>
            {
                builder.RegisterDispatch<PublishedEvent>();
                builder.RegisterDispatch<PublishedEvent>();
            });
        });

        var exception = await Assert.ThrowsAsync<MultiplePublicationRegistrationException>(async () =>
        {
            await composer.Compose();
        });
        exception.Message.Should().Contain("PublishedEvent|Queue|queueName");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(1)]
    [InlineData(5)]
    public async Task OutgoingCustomizersTests_AllCustomizersForEventWasCalled(int countOfEventsThrown)
    {
        var customizedMessage = new List<ServiceBusMessage>();
        var customizedPayload = new List<object>();
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch().ToQueue("queueName", builder =>
            {
                builder.RegisterDispatch<TestEvent>()
                    .CustomizeOutgoingMessage((a, b) =>
                    {
                        customizedMessage.Add(a);
                        customizedPayload.Add(b);
                    });
            });
        });
        composer.WithDispatchQueueSender("queueName");

        await composer.Compose();

        // Act
        var eventPublisher =
            composer.Provider.GetService(typeof(IMessagePublisher)) as IMessagePublisher;
        var eventDispatcher =
            composer.Provider.GetService(typeof(IMessageDispatcher)) as IMessageDispatcher;

        for (int i = 0; i <countOfEventsThrown; i++)
        {
            eventPublisher.Publish(new TestEvent() {EventRootId = i});
        }

        await eventDispatcher.ExecuteDispatches(CancellationToken.None);

        // Assert
        Assert.All(customizedMessage, Assert.NotNull);
        Assert.All(customizedPayload, Assert.NotNull);
        for (int i = 0; i < customizedPayload.Count; i++)
        {
            Assert.IsType<TestEvent>(customizedPayload[i]);
            Assert.Equal(((TestEvent) customizedPayload[i]).EventRootId, i);
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(1)]
    public async Task OutgoingCustomizersTests_WhenOtherEventPublished_NoCustomizersForEventWasCalled(int countOfEventsThrown)
    {
        var customizedMessage = new List<ServiceBusMessage>();
        var customizedPayload = new List<object>();
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch().ToQueue("queueName", builder =>
            {
                builder.RegisterDispatch<TestEvent>();
                builder.RegisterDispatch<PublishedEvent>()
                    .CustomizeOutgoingMessage((a, b) =>
                    {
                        customizedMessage.Add(a);
                        customizedPayload.Add(b);
                    });
            });
        }).WithDispatchQueueSender("queueName");

        await composer.Compose();

        // Act
        var eventPublisher =
            composer.Provider.GetService(typeof(IMessagePublisher)) as IMessagePublisher;
        var eventDispatcher =
            composer.Provider.GetService(typeof(IMessageDispatcher)) as IMessageDispatcher;

        for (int i = 0; i <countOfEventsThrown; i++)
        {
            eventPublisher.Publish(new TestEvent() {EventRootId = i});
        }

        await eventDispatcher.ExecuteDispatches(CancellationToken.None);

        // Assert
        Assert.Empty(customizedMessage);
        Assert.Empty(customizedPayload);
    }

    [Fact]
    public void SendToTopic_ArgumentCannotBeNull()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(_ => {});

        Assert.Throws<ArgumentNullException>(() =>
        {
            services.RegisterServiceBusDispatch().ToTopic(null, builder => {});
        });
    }

    [Fact]
    public void SendToQueue_ArgumentCannotBeNull()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(_ => {});

        Assert.Throws<ArgumentNullException>(() =>
        {
            services.RegisterServiceBusDispatch().ToQueue(null, builder => {});
        });
    }

    [Fact]
    public async Task CustomizeConnection_ChangesAreAppliedToClient()
    {
        var composer = new Composer();
        var serviceBusClientOptions = new ServiceBusClientOptions()
        {
            EnableCrossEntityTransactions = true,
            TransportType = ServiceBusTransportType.AmqpWebSockets
        };

        var factory = new Mock<IClientFactory>();
        factory.Setup(o => o.Create(It.Is<ConnectionSettings>(settings =>
                settings.ConnectionString == "Endpoint=newConnectionString;"
                && settings.Options == serviceBusClientOptions
                && settings.Endpoint == "newConnectionString")))
            .Returns(new Mock<ServiceBusClient>().Object);

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusDispatch()
                .ToTopic("testTopic", builder =>
                {
                    builder.CustomizeConnection("Endpoint=newConnectionString;", serviceBusClientOptions);
                    builder.RegisterDispatch<PublishedEvent>();
                });
            services.OverrideClientFactory(factory.Object);
        });

        await composer.Compose();
        factory.VerifyAll();
    }

    public class TestEvent
    {
        public string PayloadTypeId { get; set; } = "testEvent";
        public int EventRootId { get; set; }
    }

    public class PublishedEvent { }
}