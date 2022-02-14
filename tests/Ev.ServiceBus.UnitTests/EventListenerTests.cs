﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core.Amqp;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class EventListenerTests
    {
        [Fact]
        public async Task CanListenToQueueEvents()
        {
            var mock = new Mock<IServiceBusEventListener>();
            var composer = new Composer();

            composer.WithAdditionalOptions(builder =>
            {
                builder.RegisterEventListener<FakeListener>();
            });

            composer.WithAdditionalServices(services =>
            {
                services.AddSingleton(mock);
                services.RegisterServiceBusReception().FromQueue("testQueue", builder =>
                {
                    builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
                });

            });
            await composer.Compose();

            var clientMock = composer.Provider.GetProcessorMock("testQueue");
            var result = composer.Provider.GetRequiredService<IMessagePayloadSerializer>().SerializeBody(new SubscribedEvent());
            var message = MessageHelper.CreateMessage(result.ContentType, result.Body, nameof(SubscribedEvent));

            await clientMock.TriggerMessageReception(message, CancellationToken.None);

            mock.Verify(o => o.OnExecutionStart(It.Is<ExecutionStartedArgs>(e => e.ClientType == ClientType.Queue && e.MessageHandlerType == typeof(MessageReceptionHandler))), Times.Once);
            mock.Verify(o => o.OnExecutionSuccess(It.Is<ExecutionSucceededArgs>(e => e.ClientType == ClientType.Queue && e.MessageHandlerType == typeof(MessageReceptionHandler) && e.ExecutionDurationMilliseconds > 0)), Times.Once);
            mock.Verify(o => o.OnExecutionFailed(It.IsAny<ExecutionFailedArgs>()), Times.Never);
        }

        [Fact]
        public async Task CanListenToQueueEventsWhenThrowingExceptions()
        {
            var mock = new Mock<IServiceBusEventListener>();
            var composer = new Composer();

            composer.WithAdditionalOptions(builder =>
            {
                builder.RegisterEventListener<FakeListener>();
            });

            composer.WithAdditionalServices(services =>
            {
                services.AddSingleton(mock);
                services.RegisterServiceBusReception().FromQueue("testQueue", builder =>
                {
                    builder.RegisterReception<SubscribedEvent, SubscribedEventThrowingHandler>();
                });

            });
            await composer.Compose();

            var clientMock = composer.Provider.GetProcessorMock("testQueue");
            var result = composer.Provider.GetRequiredService<IMessagePayloadSerializer>().SerializeBody(new SubscribedEvent());
            var message = MessageHelper.CreateMessage(result.ContentType, result.Body, nameof(SubscribedEvent));

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                async () =>
                {
                    await clientMock.TriggerMessageReception(message, CancellationToken.None);
                });

            mock.Verify(o => o.OnExecutionStart(It.Is<ExecutionStartedArgs>(e => e.ClientType == ClientType.Queue && e.MessageHandlerType == typeof(MessageReceptionHandler))), Times.Once);
            mock.Verify(o => o.OnExecutionSuccess(It.IsAny<ExecutionSucceededArgs>()), Times.Never);
            mock.Verify(o => o.OnExecutionFailed(It.Is<ExecutionFailedArgs>(e => e.ClientType == ClientType.Queue && e.MessageHandlerType == typeof(MessageReceptionHandler) && e.Exception is ArgumentOutOfRangeException)), Times.Once);
        }

        [Fact]
        public async Task CanListenToSubscriptionEvents()
        {
            var mock = new Mock<IServiceBusEventListener>();
            var composer = new Composer();

            composer.WithAdditionalOptions(builder =>
            {
                builder.RegisterEventListener<FakeListener>();
            });

            composer.WithAdditionalServices(services =>
            {
                services.AddSingleton(mock);
                services.RegisterServiceBusReception().FromSubscription("testTopic", "testSubscription", builder =>
                {
                    builder.RegisterReception<SubscribedEvent, SubscribedEventHandler>();
                });

            });
            await composer.Compose();

            var clientMock = composer.Provider.GetProcessorMock("testTopic", "testSubscription");
            var result = composer.Provider.GetRequiredService<IMessagePayloadSerializer>().SerializeBody(new SubscribedEvent());
            var message = MessageHelper.CreateMessage(result.ContentType, result.Body, nameof(SubscribedEvent));

            await clientMock.TriggerMessageReception(message, CancellationToken.None);

            mock.Verify(o => o.OnExecutionStart(It.Is<ExecutionStartedArgs>(e => e.ClientType == ClientType.Subscription && e.MessageHandlerType == typeof(MessageReceptionHandler))), Times.Once);
            mock.Verify(o => o.OnExecutionSuccess(It.Is<ExecutionSucceededArgs>(e => e.ClientType == ClientType.Subscription && e.MessageHandlerType == typeof(MessageReceptionHandler) && e.ExecutionDurationMilliseconds > 0)), Times.Once);
            mock.Verify(o => o.OnExecutionFailed(It.IsAny<ExecutionFailedArgs>()), Times.Never);
        }

        [Fact]
        public async Task CanListenToSubscriptionEventsWhenThrowingExceptions()
        {
            var mock = new Mock<IServiceBusEventListener>();
            var composer = new Composer();

            composer.WithAdditionalOptions(builder =>
            {
                builder.RegisterEventListener<FakeListener>();
            });

            composer.WithAdditionalServices(services =>
            {
                services.AddSingleton(mock);
                services.RegisterServiceBusReception().FromSubscription("testTopic", "testSubscription", builder =>
                {
                    builder.RegisterReception<SubscribedEvent, SubscribedEventThrowingHandler>();
                });

            });
            await composer.Compose();

            var clientMock = composer.Provider.GetProcessorMock("testTopic", "testSubscription");
            var result = composer.Provider.GetRequiredService<IMessagePayloadSerializer>().SerializeBody(new SubscribedEvent());
            var message = MessageHelper.CreateMessage(result.ContentType, result.Body, nameof(SubscribedEvent));

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                async () =>
                {
                    await clientMock.TriggerMessageReception(message, CancellationToken.None);
                });

            mock.Verify(o => o.OnExecutionStart(It.Is<ExecutionStartedArgs>(e => e.ClientType == ClientType.Subscription && e.MessageHandlerType == typeof(MessageReceptionHandler))), Times.Once);
            mock.Verify(o => o.OnExecutionSuccess(It.IsAny<ExecutionSucceededArgs>()), Times.Never);
            mock.Verify(o => o.OnExecutionFailed(It.Is<ExecutionFailedArgs>(e => e.ClientType == ClientType.Subscription && e.MessageHandlerType == typeof(MessageReceptionHandler) && e.Exception is ArgumentOutOfRangeException)), Times.Once);
        }

    }

    public class FakeListener : IServiceBusEventListener
    {
        public Mock<IServiceBusEventListener> Mock { get; }

        public FakeListener(Mock<IServiceBusEventListener> mock)
        {
            Mock = mock;
        }

        public Task OnExecutionStart(ExecutionStartedArgs args)
        {
            return Mock.Object.OnExecutionStart(args);
        }

        public Task OnExecutionSuccess(ExecutionSucceededArgs args)
        {
            return Mock.Object.OnExecutionSuccess(args);
        }

        public Task OnExecutionFailed(ExecutionFailedArgs args)
        {
            return Mock.Object.OnExecutionFailed(args);
        }
    }
}
