using System;
using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Exceptions;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests;

public class EventListenerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("customPropertyName")]
    public async Task CanListenToQueueEvents(string customPropertyName)
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
                builder.WithCustomPayloadTypeIdProperty(customPropertyName);
            });

        });
        await composer.Compose();

        var clientMock = composer.Provider.GetProcessorMock("testQueue");
        var result = composer.Provider.GetRequiredService<IMessagePayloadSerializer>().SerializeBody(new SubscribedEvent());
        var message = MessageHelper.CreateMessage(result.ContentType, result.Body, nameof(SubscribedEvent), customPropertyName);

        await clientMock.TriggerMessageReception(message, CancellationToken.None);

        mock.Verify(o => o.OnExecutionStart(It.Is<ExecutionStartedArgs>(e => e.ClientType == ClientType.Queue)), Times.Once);
        mock.Verify(o => o.OnExecutionSuccess(It.Is<ExecutionSucceededArgs>(e => e.ClientType == ClientType.Queue && e.ExecutionDurationMilliseconds > 0)), Times.Once);
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
        var message = MessageHelper.CreateMessage(result.ContentType, result.Body, nameof(SubscribedEvent), null);
        message.MessageId = Guid.NewGuid().ToString();

        var action = async () =>
        {
            await clientMock.TriggerMessageReception(message, CancellationToken.None);
        };

        using (new AssertionScope())
        {
            var exception = await action.Should().ThrowAsync<FailedToProcessMessageException>();
            exception.WithInnerException<ArgumentOutOfRangeException>();
            exception.And.ClientType.Should().Be("Queue");
            exception.And.ResourceId.Should().Be("testQueue");
            exception.And.MessageId.Should().Be(message.MessageId);
            exception.And.HandlerName.Should().Be("Ev.ServiceBus.UnitTests.Helpers.SubscribedEventThrowingHandler");
            exception.And.SessionId.Should().Be("none");
            exception.And.PayloadTypeId.Should().Be("SubscribedEvent");
            mock.Verify(o => o.OnExecutionStart(It.Is<ExecutionStartedArgs>(e => e.ClientType == ClientType.Queue)), Times.Once);
            mock.Verify(o => o.OnExecutionSuccess(It.IsAny<ExecutionSucceededArgs>()), Times.Never);
            mock.Verify(o => o.OnExecutionFailed(It.Is<ExecutionFailedArgs>(e => e.ClientType == ClientType.Queue && e.Exception is ArgumentOutOfRangeException)), Times.Once);
        }
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
        var message = MessageHelper.CreateMessage(result.ContentType, result.Body, nameof(SubscribedEvent), null);

        await clientMock.TriggerMessageReception(message, CancellationToken.None);

        mock.Verify(o => o.OnExecutionStart(It.Is<ExecutionStartedArgs>(e => e.ClientType == ClientType.Subscription)), Times.Once);
        mock.Verify(o => o.OnExecutionSuccess(It.Is<ExecutionSucceededArgs>(e => e.ClientType == ClientType.Subscription && e.ExecutionDurationMilliseconds > 0)), Times.Once);
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
        var message = MessageHelper.CreateMessage(result.ContentType, result.Body, nameof(SubscribedEvent), null);
        message.MessageId = Guid.NewGuid().ToString();

        var action = async () =>
        {
            await clientMock.TriggerMessageReception(message, CancellationToken.None);
        };

        using (new AssertionScope())
        {
            var exception = await action.Should().ThrowAsync<FailedToProcessMessageException>();
            exception.WithInnerException<ArgumentOutOfRangeException>();
            exception.And.ClientType.Should().Be("Subscription");
            exception.And.ResourceId.Should().Be("testTopic/Subscriptions/testSubscription");
            exception.And.MessageId.Should().Be(message.MessageId);
            exception.And.HandlerName.Should().Be("Ev.ServiceBus.UnitTests.Helpers.SubscribedEventThrowingHandler");
            exception.And.SessionId.Should().Be("none");
            exception.And.PayloadTypeId.Should().Be("SubscribedEvent");
            mock.Verify(o => o.OnExecutionStart(It.Is<ExecutionStartedArgs>(e => e.ClientType == ClientType.Subscription)), Times.Once);
            mock.Verify(o => o.OnExecutionSuccess(It.IsAny<ExecutionSucceededArgs>()), Times.Never);
            mock.Verify(o => o.OnExecutionFailed(It.Is<ExecutionFailedArgs>(e => e.ClientType == ClientType.Subscription && e.Exception is ArgumentOutOfRangeException)), Times.Once);
        }
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