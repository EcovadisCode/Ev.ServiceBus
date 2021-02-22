﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Exceptions;
using Ev.ServiceBus.Dispatch;
using Ev.ServiceBus.Management;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Composer = Ev.ServiceBus.UnitTests.Helpers.Composer;

namespace Ev.ServiceBus.UnitTests
{
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

            var registry = composer.Provider.GetRequiredService<IServiceBusRegistry>() as ServiceBusRegistry;
            var senders = registry!.GetAllSenders();
            senders.Length.Should().Be(1);
            senders.First().ResourceId.Should().Be("topicName");
            senders.First().ClientType.Should().Be(ClientType.Topic);
        }

        [Fact]
        public async Task CanRegisterSameTopicSenderTwice_Case2()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusTopic("topicName");
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

            var registry = composer.Provider.GetRequiredService<IServiceBusRegistry>() as ServiceBusRegistry;
            var senders = registry!.GetAllSenders();
            senders.Length.Should().Be(1);
            senders.First().ResourceId.Should().Be("topicName");
            senders.First().ClientType.Should().Be(ClientType.Topic);
        }

        [Fact]
        public async Task CanRegisterSameTopicSenderTwice_Case3()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusTopic("topicName").WithConnection("anotherConnectionString");

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

            var registry = composer.Provider.GetRequiredService<IServiceBusRegistry>() as ServiceBusRegistry;
            var senders = registry!.GetAllSenders();
            senders.Length.Should().Be(2);
            senders.Should().SatisfyRespectively(
                sender =>
                {
                    sender.ResourceId.Should().Be("topicName");
                    sender.ClientType.Should().Be(ClientType.Topic);
                },
                sender =>
                {
                    sender.ResourceId.Should().Be("topicName_2");
                    sender.ClientType.Should().Be(ClientType.Topic);
                });
        }

        [Fact]
        public async Task CanRegisterSameTopicSenderTwice_Case4()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusTopic("topicName");

                services.RegisterServiceBusDispatch().ToTopic("topicName", builder =>
                {
                    builder.CustomizeConnection("anotherConnectionString");
                    builder.RegisterDispatch<PublishedEvent>();
                });
                services.RegisterServiceBusDispatch().ToTopic("topicName", builder =>
                {
                    builder.RegisterDispatch<NoiseEvent>();
                });
            });

            await composer.Compose();

            var registry = composer.Provider.GetRequiredService<IServiceBusRegistry>() as ServiceBusRegistry;
            var senders = registry!.GetAllSenders();
            senders.Length.Should().Be(2);
            senders.Should().SatisfyRespectively(
                sender =>
                {
                    sender.ResourceId.Should().Be("topicName");
                    sender.ClientType.Should().Be(ClientType.Topic);
                },
                sender =>
                {
                    sender.ResourceId.Should().Be("topicName_2");
                    sender.ClientType.Should().Be(ClientType.Topic);
                });
        }

        [Fact]
        public async Task CanRegisterSameTopicSenderTwice_Case5()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusTopic("topicName").WithConnection("anotherConnectionString");

                services.RegisterServiceBusDispatch().ToTopic("topicName", builder =>
                {
                    builder.CustomizeConnection("anotherConnectionString2");
                    builder.RegisterDispatch<PublishedEvent>();
                });
                services.RegisterServiceBusDispatch().ToTopic("topicName", builder =>
                {
                    builder.RegisterDispatch<NoiseEvent>();
                });
            });

            await composer.Compose();

            var registry = composer.Provider.GetRequiredService<IServiceBusRegistry>() as ServiceBusRegistry;
            var senders = registry!.GetAllSenders();
            senders.Length.Should().Be(3);
            senders.Should().SatisfyRespectively(
                sender =>
                {
                    sender.ResourceId.Should().Be("topicName");
                    sender.ClientType.Should().Be(ClientType.Topic);
                },
                sender =>
                {
                    sender.ResourceId.Should().Be("topicName_2");
                    sender.ClientType.Should().Be(ClientType.Topic);
                },
                sender =>
                {
                    sender.ResourceId.Should().Be("topicName_3");
                    sender.ClientType.Should().Be(ClientType.Topic);
                });
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

            var registry = composer.Provider.GetRequiredService<IServiceBusRegistry>() as ServiceBusRegistry;
            var senders = registry!.GetAllSenders();
            senders.Length.Should().Be(1);
            senders.First().ResourceId.Should().Be("queue");
            senders.First().ClientType.Should().Be(ClientType.Queue);
        }

        [Fact]
        public async Task CanRegisterSameQueueSenderTwice_Case2()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusQueue("queueName");
                services.RegisterServiceBusDispatch().ToQueue("queueName", builder =>
                {
                    builder.RegisterDispatch<PublishedEvent>();
                });
                services.RegisterServiceBusDispatch().ToQueue("queueName", builder =>
                {
                    builder.RegisterDispatch<NoiseEvent>();
                });
            });

            await composer.Compose();

            var registry = composer.Provider.GetRequiredService<IServiceBusRegistry>() as ServiceBusRegistry;
            var senders = registry!.GetAllSenders();
            senders.Length.Should().Be(1);
            senders.First().ResourceId.Should().Be("queueName");
            senders.First().ClientType.Should().Be(ClientType.Queue);
        }

        [Fact]
        public async Task CanRegisterSameQueueSenderTwice_Case3()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusQueue("queueName").WithConnection("anotherConnectionString");

                services.RegisterServiceBusDispatch().ToQueue("queueName", builder =>
                {
                    builder.RegisterDispatch<PublishedEvent>();
                });
                services.RegisterServiceBusDispatch().ToQueue("queueName", builder =>
                {
                    builder.RegisterDispatch<NoiseEvent>();
                });
            });

            await composer.Compose();

            var registry = composer.Provider.GetRequiredService<IServiceBusRegistry>() as ServiceBusRegistry;
            var senders = registry!.GetAllSenders();
            senders.Length.Should().Be(2);
            senders.Should().SatisfyRespectively(
                sender =>
                {
                    sender.ResourceId.Should().Be("queueName");
                    sender.ClientType.Should().Be(ClientType.Queue);
                },
                sender =>
                {
                    sender.ResourceId.Should().Be("queueName_2");
                    sender.ClientType.Should().Be(ClientType.Queue);
                });
        }

        [Fact]
        public async Task CanRegisterSameQueueSenderTwice_Case4()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusQueue("queueName");

                services.RegisterServiceBusDispatch().ToQueue("queueName", builder =>
                {
                    builder.CustomizeConnection("anotherConnectionString");
                    builder.RegisterDispatch<PublishedEvent>();
                });
                services.RegisterServiceBusDispatch().ToQueue("queueName", builder =>
                {
                    builder.RegisterDispatch<NoiseEvent>();
                });
            });

            await composer.Compose();

            var registry = composer.Provider.GetRequiredService<IServiceBusRegistry>() as ServiceBusRegistry;
            var senders = registry!.GetAllSenders();
            senders.Length.Should().Be(2);
            senders.Should().SatisfyRespectively(
                sender =>
                {
                    sender.ResourceId.Should().Be("queueName");
                    sender.ClientType.Should().Be(ClientType.Queue);
                },
                sender =>
                {
                    sender.ResourceId.Should().Be("queueName_2");
                    sender.ClientType.Should().Be(ClientType.Queue);
                });
        }

        [Fact]
        public async Task CanRegisterSameQueueSenderTwice_Case5()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusQueue("queueName").WithConnection("anotherConnectionString");

                services.RegisterServiceBusDispatch().ToQueue("queueName", builder =>
                {
                    builder.CustomizeConnection("anotherConnectionString2");
                    builder.RegisterDispatch<PublishedEvent>();
                });
                services.RegisterServiceBusDispatch().ToQueue("queueName", builder =>
                {
                    builder.RegisterDispatch<NoiseEvent>();
                });
            });

            await composer.Compose();

            var registry = composer.Provider.GetRequiredService<IServiceBusRegistry>() as ServiceBusRegistry;
            var senders = registry!.GetAllSenders();
            senders.Length.Should().Be(3);
            senders.Should().SatisfyRespectively(
                sender =>
                {
                    sender.ResourceId.Should().Be("queueName");
                    sender.ClientType.Should().Be(ClientType.Queue);
                },
                sender =>
                {
                    sender.ResourceId.Should().Be("queueName_2");
                    sender.ClientType.Should().Be(ClientType.Queue);
                },
                sender =>
                {
                    sender.ResourceId.Should().Be("queueName_3");
                    sender.ClientType.Should().Be(ClientType.Queue);
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

            await composer.Compose();

            var exception = Assert.Throws<MultiplePublicationRegistrationException>(() =>
            {
                composer.Provider.GetService(typeof(DispatchRegistry));
            });
            exception.Message.Should().Contain("PublishedEvent|Queue|queueName");
        }

        [Theory]
        [InlineData(2)]
        [InlineData(1)]
        [InlineData(5)]
        public async Task OutgoingCustomizersTests_AllCustomizersForEventWasCalled(int countOfEventsThrown)
        {
            var customizedMessage = new List<Message>();
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

            await eventDispatcher.DispatchEvents();

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
            var customizedMessage = new List<Message>();
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

            await eventDispatcher.DispatchEvents();

            // Assert
            Assert.Empty(customizedMessage);
            Assert.Empty(customizedPayload);
        }

        [Fact]
        public void SendToTopic_ArgumentCannotBeNull()
        {
            var services = new ServiceCollection();

            services.AddServiceBus<PayloadParser>(_ => {});

            Assert.Throws<ArgumentNullException>(() =>
            {
                services.RegisterServiceBusDispatch().ToTopic(null, builder => {});
            });
        }

        [Fact]
        public void SendToQueue_ArgumentCannotBeNull()
        {
            var services = new ServiceCollection();

            services.AddServiceBus<PayloadParser>(_ => {});

            Assert.Throws<ArgumentNullException>(() =>
            {
                services.RegisterServiceBusDispatch().ToQueue(null, builder => {});
            });
        }

        [Fact]
        public async Task CustomizeConnection_ChangesAreAppliedToClient()
        {
            var composer = new Composer();

            var factory = new Mock<IClientFactory<TopicOptions, ITopicClient>>();
            factory.Setup(o => o.Create(
                    It.IsAny<TopicOptions>(),
                    It.Is<ConnectionSettings>(settings => settings.ConnectionString == "newConnectionString"
                                                          && settings.ReceiveMode == ReceiveMode.ReceiveAndDelete)))
                .Returns(new Mock<ITopicClient>().Object);

            composer.WithAdditionalServices(services =>
            {
                services.RegisterServiceBusDispatch()
                    .ToTopic("testTopic", builder =>
                    {
                        builder.CustomizeConnection("newConnectionString", ReceiveMode.ReceiveAndDelete, RetryPolicy.NoRetry);
                        builder.RegisterDispatch<PublishedEvent>();
                    });
                ServiceCollectionHelpers.OverrideClientFactory(services, factory.Object);
            });

            await composer.Compose();
            factory.VerifyAll();
        }

        public class TestEvent
        {
            public string EventTypeId { get; set; } = "testEvent";
            public int EventRootId { get; set; }
        }

        public class PublishedEvent { }
    }
}
