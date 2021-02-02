using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ev.ServiceBus.IntegrationEvents.Publication;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.IntegrationEvents.UnitTests.Helpers;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Ev.ServiceBus.IntegrationEvents.UnitTests
{
    public class PublicationTest : IDisposable
    {
        private readonly Composer _composer;
        private readonly List<Message> _sentMessagesToTopic;
        private readonly List<Message> _sentMessagesToQueue;

        public PublicationTest()
        {
            _sentMessagesToTopic = new List<Message>();
            _sentMessagesToQueue = new List<Message>();
            _composer = new Composer();

            _composer.WithAdditionalServices(services =>
            {
                    services.RegisterServiceBusTopic("testTopic").WithConnection("testConnectionString");
                    services.RegisterServiceBusQueue("testQueue").WithConnection("testConnectionString");

                    // noise
                    services.RegisterServiceBusTopic("testTopic2").WithConnection("testConnectionString");
                    services.RegisterServiceBusQueue("testQueue2").WithConnection("testConnectionString");

                services.RegisterIntegrationEventPublication<PublishedEvent>(builder =>
                {
                    builder.EventTypeId = "MyEvent";
                    builder.SendToTopic("testTopic");
                });
                services.RegisterIntegrationEventPublication<PublishedThroughQueueEvent>(builder =>
                {
                    builder.EventTypeId = "MyEventThroughQueue";
                    builder.SendToQueue("testQueue");
                });

                // noise
                services.RegisterIntegrationEventPublication<PublishedEvent2>(builder =>
                {
                    builder.EventTypeId = "MyEvent2";
                    builder.SendToTopic("testTopic2");
                });
                services.RegisterIntegrationEventPublication<PublishedEvent3>(builder =>
                {
                    builder.EventTypeId = "MyEvent3";
                    builder.SendToTopic("testTopic");
                });

            });

            _composer.Compose().GetAwaiter().GetResult();

            var topicClient = _composer.TopicFactory.GetAllRegisteredTopicClients().First();
            topicClient.Mock
                .Setup(o => o.SendAsync(It.IsAny<Message>()))
                .Returns(Task.CompletedTask)
                .Callback((Message message) =>
                {
                    _sentMessagesToTopic.Add(message);
                });

            topicClient.Mock
                .Setup(o => o.SendAsync(It.IsAny<IList<Message>>()))
                .Returns(Task.CompletedTask)
                .Callback((IList<Message> messages) =>
                {
                    _sentMessagesToTopic.AddRange(messages);
                });

            var queueClient = _composer.QueueFactory.GetAllRegisteredQueueClients().First();
            queueClient.Mock
                .Setup(o => o.SendAsync(It.IsAny<Message>()))
                .Returns(Task.CompletedTask)
                .Callback((Message message) =>
                {
                    _sentMessagesToQueue.Add(message);
                });

            queueClient.Mock
                .Setup(o => o.SendAsync(It.IsAny<IList<Message>>()))
                .Returns(Task.CompletedTask)
                .Callback((IList<Message> messages) =>
                {
                    _sentMessagesToQueue.AddRange(messages);
                });

            SimulatePublication().GetAwaiter().GetResult();
        }

        private async Task SimulatePublication()
        {
            using (var scope = _composer.Provider.CreateScope())
            {
                var eventPublisher = scope.ServiceProvider.GetService<IIntegrationEventPublisher>();
                var eventDispatcher = scope.ServiceProvider.GetService<IIntegrationEventDispatcher>();

                eventPublisher.Publish(new PublishedEvent()
                {
                    SomeNumber = 36,
                    SomeString = "hello"
                });
                eventPublisher.Publish(new PublishedThroughQueueEvent()
                {
                    SomeNumber = 36,
                    SomeString = "hello"
                });

                await eventDispatcher.DispatchEvents();
            }
        }

        public class PublishedEvent
        {
            public string SomeString { get; set; }
            public int SomeNumber { get; set; }
        }

        public class PublishedThroughQueueEvent : PublishedEvent { }

        public class PublishedEvent2 { }
        public class PublishedEvent3 { }

        [Theory]
        [InlineData("topic")]
        [InlineData("queue")]
        public void MessageMustBeSentToTheConfiguredTopic(string clientToCheck)
        {
            Assert.NotNull(GetMessageFrom(clientToCheck));
        }

        [Theory]
        [InlineData("topic")]
        [InlineData("queue")]
        public void MessageMustContainTheRightMessageType(string clientToCheck)
        {
            var message = GetMessageFrom(clientToCheck);
            Assert.True(message?.UserProperties.ContainsKey("MessageType"));
            Assert.Equal("IntegrationEvent", message?.UserProperties["MessageType"]);
        }

        [Theory]
        [InlineData("topic", "MyEvent")]
        [InlineData("queue", "MyEventThroughQueue")]
        public void MessageMustContainTheRightEventTypeId(string clientToCheck, string eventTypeId)
        {
            var message = GetMessageFrom(clientToCheck);
            Assert.True(message?.UserProperties.ContainsKey("EventTypeId"));
            Assert.Equal(eventTypeId, message?.UserProperties["EventTypeId"]);
        }

        [Theory]
        [InlineData("topic")]
        [InlineData("queue")]
        public void MessageContentTypeMustBeSet(string clientToCheck)
        {
            var message = GetMessageFrom(clientToCheck);
            Assert.Equal("application/json", message?.ContentType);
        }

        [Theory]
        [InlineData("topic", typeof(PublishedEvent))]
        [InlineData("queue", typeof(PublishedThroughQueueEvent))]
        public void MessageMustContainAProperJsonBody(string clientToCheck, Type typeToParse)
        {
            var message = GetMessageFrom(clientToCheck);
            var body = Encoding.UTF8.GetString(message?.Body);
            var @event = JsonConvert.DeserializeObject(body, typeToParse) as PublishedEvent;
            Assert.NotNull(@event);
            Assert.Equal("hello", @event.SomeString);
            Assert.Equal(36, @event.SomeNumber);
        }

        [Theory]
        [InlineData("topic")]
        [InlineData("queue")]
        public void MessageMustContainALabel(string clientToCheck)
        {
            var message = GetMessageFrom(clientToCheck);
            Assert.NotNull(message?.Label);
        }

        private Message GetMessageFrom(string clientToCheck)
        {
            if (clientToCheck == "topic")
            {
                return _sentMessagesToTopic.FirstOrDefault();
            }
            return _sentMessagesToQueue.FirstOrDefault();
        }

        public void Dispose()
        {
            _composer?.Dispose();
        }
    }
}
