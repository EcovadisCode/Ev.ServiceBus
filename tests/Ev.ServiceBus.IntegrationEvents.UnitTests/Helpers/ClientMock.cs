using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Moq;

namespace Ev.ServiceBus.IntegrationEvents.UnitTests.Helpers
{
    public class QueueClientMock
    {
        private Func<ExceptionReceivedEventArgs, Task> _triggerExceptionOccured = args => Task.CompletedTask;
        private Func<Message, CancellationToken, Task> _triggerMessageReception = (m, t) => Task.CompletedTask;

        public QueueClientMock(string name)
        {
            Mock = new Mock<IQueueClient>();
            Mock
                .Setup(o => o.RegisterMessageHandler(
                    It.IsAny<Func<Message, CancellationToken, Task>>(),
                    It.IsAny<MessageHandlerOptions>()))
                .Callback((Func<Message, CancellationToken, Task> messageHandler, MessageHandlerOptions options) =>
                {
                    _triggerMessageReception = messageHandler;
                    _triggerExceptionOccured = options.ExceptionReceivedHandler;
                });
            Mock.SetupGet(o => o.QueueName).Returns(name);
            QueueName = name;
        }

        public string QueueName { get; }
        public IQueueClient QueueClient => Mock.Object;
        public Mock<IQueueClient> Mock { get; }

        public Task TriggerMessageReception(Message message, CancellationToken token)
        {
            return _triggerMessageReception(message, token);
        }

        public Task TriggerExceptionOccured(ExceptionReceivedEventArgs args)
        {
            return _triggerExceptionOccured(args);
        }
    }

    public class TopicClientMock
    {
        public TopicClientMock(string name)
        {
            Mock = new Mock<ITopicClient>();
            Mock.SetupGet(o => o.TopicName).Returns(name);
            ClientName = name;
        }

        public Mock<ITopicClient> Mock { get; }

        public string ClientName { get; }
        public ITopicClient Client => Mock.Object;
    }

    public class SubscriptionClientMock
    {
        private readonly Mock<ISubscriptionClient> _client;
        private Func<ExceptionReceivedEventArgs, Task> _triggerExceptionOccured = args => Task.CompletedTask;
        private Func<Message, CancellationToken, Task> _triggerMessageReception = (m, t) => Task.CompletedTask;

        public SubscriptionClientMock(string name)
        {
            _client = new Mock<ISubscriptionClient>();

            _client.Setup(o => o.CompleteAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            _client.Setup(o => o.AbandonAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            _client
                .Setup(o => o.RegisterMessageHandler(
                    It.IsAny<Func<Message, CancellationToken, Task>>(),
                    It.IsAny<MessageHandlerOptions>()))
                .Callback((Func<Message, CancellationToken, Task> messageHandler, MessageHandlerOptions options) =>
                {
                    _triggerMessageReception = messageHandler;
                    _triggerExceptionOccured = options.ExceptionReceivedHandler;
                });

            _client.SetupGet(o => o.SubscriptionName).Returns(name);

            ClientName = name;
        }

        public string ClientName { get; }
        public ISubscriptionClient Client => _client.Object;
        public Mock<ISubscriptionClient> Mock => _client;

        public Task TriggerMessageReception(Message message, CancellationToken token)
        {
            return _triggerMessageReception(message, token);
        }

        public Task TriggerExceptionOccured(ExceptionReceivedEventArgs args)
        {
            return _triggerExceptionOccured(args);
        }
    }
}
