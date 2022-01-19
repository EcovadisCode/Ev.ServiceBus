using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Moq;

namespace Ev.ServiceBus.UnitTests.Helpers
{
    public class QueueClientMock
    {
        private readonly Mock<IQueueClient> _client;
        private Func<ExceptionReceivedEventArgs, Task> _triggerExceptionOccured = (args) => Task.CompletedTask;
        private Func<Message, CancellationToken, Task> _triggerMessageReception = (m, t) => Task.CompletedTask;
        private Func<ExceptionReceivedEventArgs, Task> _triggerSessionExceptionOccured = args => Task.CompletedTask;
        private Func<IMessageSession, Message, CancellationToken, Task> _triggerSessionMessageReception = (s, m, t) => Task.CompletedTask;

        public QueueClientMock(string name)
        {
            _client = new Mock<IQueueClient>();
            _client
               .Setup(o => o.RegisterMessageHandler(It.IsAny<Func<Message, CancellationToken, Task>>(), It.IsAny<MessageHandlerOptions>()))
               .Callback((Func<Message, CancellationToken, Task> messageHandler, MessageHandlerOptions options) =>
               {
                   IsReceiver = true;
                   _triggerMessageReception = messageHandler;
                   _triggerExceptionOccured = options.ExceptionReceivedHandler;
                });
            _client
                .Setup(o => o.RegisterSessionHandler(It.IsAny<Func<IMessageSession, Message, CancellationToken, Task>>(), It.IsAny<SessionHandlerOptions>()))
                .Callback((Func<IMessageSession, Message, CancellationToken, Task> messageHandler, SessionHandlerOptions options) =>
                {
                    IsReceiver = true;
                    _triggerSessionMessageReception = messageHandler;
                    _triggerSessionExceptionOccured = options.ExceptionReceivedHandler;
                });
            _client.SetupGet(o => o.QueueName).Returns(name);
            ClientName = name;
        }

        public bool IsReceiver { get; private set; }

        public string ClientName { get; }
        public IQueueClient QueueClient => _client.Object;
        public Mock<IQueueClient> Mock => _client;

        public Task TriggerMessageReception(Message message, CancellationToken token)
        {
            return _triggerMessageReception(message, token);
        }

        public Task TriggerExceptionOccured(ExceptionReceivedEventArgs args)
        {
            return _triggerExceptionOccured(args);
        }

        public Task TriggerSessionMessageReception(Message message, CancellationToken token)
        {
            return _triggerSessionMessageReception(null, message, token);
        }

        public Task TriggerSessionExceptionOccured(ExceptionReceivedEventArgs args)
        {
            return _triggerSessionExceptionOccured(args);
        }
    }

    public class TopicClientMock
    {
        private readonly Mock<ITopicClient> _client;

        public Mock<ITopicClient> Mock => _client;
        public TopicClientMock(string name)
        {
            _client = new Mock<ITopicClient>();
            _client.SetupGet(o => o.TopicName).Returns(name);
            ClientName = name;
        }
        public string ClientName { get; }
        public ITopicClient Client => _client.Object;
    }

    public class SubscriptionClientMock
    {
        private readonly Mock<ISubscriptionClient> _client;
        private Func<ExceptionReceivedEventArgs, Task> _triggerExceptionOccured = args => Task.CompletedTask;
        private Func<Message, CancellationToken, Task> _triggerMessageReception = (m, t) => Task.CompletedTask;
        private Func<ExceptionReceivedEventArgs, Task> _triggerSessionExceptionOccured = args => Task.CompletedTask;
        private Func<IMessageSession, Message, CancellationToken, Task> _triggerSessionMessageReception = (s, m, t) => Task.CompletedTask;

        public SubscriptionClientMock(string name)
        {
            _client = new Mock<ISubscriptionClient>();
            _client.SetupGet(o => o.SubscriptionName).Returns(name);
            _client
                .Setup(o => o.RegisterMessageHandler(It.IsAny<Func<Message, CancellationToken, Task>>(), It.IsAny<MessageHandlerOptions>()))
                .Callback((Func<Message, CancellationToken, Task> messageHandler, MessageHandlerOptions options) =>
                {
                    _triggerMessageReception = messageHandler;
                    _triggerExceptionOccured = options.ExceptionReceivedHandler;
                });
            _client
                .Setup(o => o.RegisterSessionHandler(It.IsAny<Func<IMessageSession, Message, CancellationToken, Task>>(), It.IsAny<SessionHandlerOptions>()))
                .Callback((Func<IMessageSession, Message, CancellationToken, Task> messageHandler, SessionHandlerOptions options) =>
                {
                    _triggerSessionMessageReception = messageHandler;
                    _triggerSessionExceptionOccured = options.ExceptionReceivedHandler;
                });

            ClientName = name;
        }

        public Mock<ISubscriptionClient> Mock => _client;
        public string ClientName { get; }
        public ISubscriptionClient Client => _client.Object;

        public Task TriggerMessageReception(Message message, CancellationToken token)
        {
            return _triggerMessageReception(message, token);
        }

        public Task TriggerExceptionOccured(ExceptionReceivedEventArgs args)
        {
            return _triggerExceptionOccured(args);
        }

        public Task TriggerSessionMessageReception(Message message, CancellationToken token)
        {
            return _triggerSessionMessageReception(null, message, token);
        }

        public Task TriggerSessionExceptionOccured(ExceptionReceivedEventArgs args)
        {
            return _triggerSessionExceptionOccured(args);
        }

    }
}
