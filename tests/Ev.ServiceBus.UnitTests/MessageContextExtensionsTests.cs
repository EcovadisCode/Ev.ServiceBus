using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Reception.Extensions;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class MessageContextExtensionsTests
    {
        [Fact]
        public async Task CompleteAndResendMessageAsync_ShouldCompleteAndResendMessageToCorrectQueue()
        {
            // Arrange
            var queueName = "test-queue";
            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();
            var receiverMock = new Mock<ServiceBusReceiver>();
            var args = new ProcessMessageEventArgs(
                receivedMessage,
                receiverMock.Object,
                CancellationToken.None);

            var messageContext = new MessageContext(
                args,
                ClientType.Queue,
                queueName);

            var metadataMock = new Mock<IMessageMetadata>();

            var metadataAccessorMock = new Mock<IMessageMetadataAccessor>();

            metadataAccessorMock.Setup(a => a.Metadata).Returns(metadataMock.Object);

            var senderMock = new Mock<ServiceBusSender>();
            var clientMock = new Mock<ServiceBusClient>();
            clientMock
                .Setup(c => c.CreateSender(queueName))
                .Returns(senderMock.Object);

            // Act
            await messageContext.CompleteAndResendMessageAsync(
                metadataAccessorMock.Object,
                clientMock.Object);

            // Assert
            metadataMock.Verify(m => m.CompleteMessageAsync(), Times.Once);
            senderMock.Verify(
                s => s.SendMessageAsync(
                    It.IsAny<ServiceBusMessage>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            clientMock.Verify(c => c.CreateSender(queueName), Times.Once);
        }

        [Fact]
        public async Task CompleteAndResendMessageAsync_WithSubscription_SendsToCorrectTopic()
        {
            // Arrange
            var topicName = "test-topic";
            var subscriptionPath = $"{topicName}/Subscriptions/test-subscription";

            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();
            var receiverMock = new Mock<ServiceBusReceiver>();
            var args = new ProcessMessageEventArgs(
                receivedMessage,
                receiverMock.Object,
                CancellationToken.None);

            var messageContext = new MessageContext(
                args,
                ClientType.Subscription,
                subscriptionPath);

            var metadataMock = new Mock<IMessageMetadata>();
            var metadataAccessorMock = new Mock<IMessageMetadataAccessor>();
            metadataAccessorMock.Setup(a => a.Metadata).Returns(metadataMock.Object);

            var senderMock = new Mock<ServiceBusSender>();
            var clientMock = new Mock<ServiceBusClient>();
            clientMock
                .Setup(c => c.CreateSender(topicName))
                .Returns(senderMock.Object);

            // Act
            await messageContext.CompleteAndResendMessageAsync(
                metadataAccessorMock.Object,
                clientMock.Object);

            // Assert
            metadataMock.Verify(m => m.CompleteMessageAsync(), Times.Once);
            senderMock.Verify(
                s => s.SendMessageAsync(
                    It.IsAny<ServiceBusMessage>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            clientMock.Verify(c => c.CreateSender(topicName), Times.Once);
        }

        [Fact]
        public async Task CompleteAndResendMessageAsync_WithInvalidClientType_ThrowsArgumentException()
        {
            // Arrange
            var resourceId = "test-resource";
            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage();
            var receiverMock = new Mock<ServiceBusReceiver>();
            var args = new ProcessMessageEventArgs(
                receivedMessage,
                receiverMock.Object,
                CancellationToken.None);

            var messageContext = new MessageContext(
                args,
                (ClientType)999,
                resourceId);

            var metadataMock = new Mock<IMessageMetadata>();
            var metadataAccessorMock = new Mock<IMessageMetadataAccessor>();
            metadataAccessorMock.Setup(a => a.Metadata).Returns(metadataMock.Object);

            var clientMock = new Mock<ServiceBusClient>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                messageContext.CompleteAndResendMessageAsync(
                    metadataAccessorMock.Object,
                    clientMock.Object));

            metadataMock.Verify(m => m.CompleteMessageAsync(), Times.Once);
            clientMock.Verify(c => c.CreateSender(It.IsAny<string>()), Times.Never);
        }
    }
}