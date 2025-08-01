using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Management;
using Microsoft.Extensions.Options;
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
            metadataMock.Setup(m => m.CompleteMessageAsync()).Returns(Task.CompletedTask);

            var metadataAccessorMock = new Mock<IMessageMetadataAccessor>();
            metadataAccessorMock.Setup(a => a.Metadata).Returns(metadataMock.Object);

            var senderMock = new Mock<ServiceBusSender>();
            senderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Create a mock for IMessageSender that will work with the registry
            var messageSenderMock = new Mock<IMessageSender>();
            messageSenderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            messageSenderMock.Setup(s => s.ClientType).Returns(ClientType.Queue);
            messageSenderMock.Setup(s => s.Name).Returns(queueName);

            // Create the registry with mocked dependencies
            var clientFactoryMock = new Mock<IClientFactory>();
            var optionsMock = new Mock<IOptions<ServiceBusOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new ServiceBusOptions());

            var registry = new ServiceBusRegistry(clientFactoryMock.Object, optionsMock.Object);

            // Register the IMessageSender mock with the registry
            var registerMethod = typeof(ServiceBusRegistry).GetMethod("Register",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                [typeof(IMessageSender)],
                null);

            registerMethod!.Invoke(registry, [messageSenderMock.Object]);

            // Act
            await messageContext.CompleteAndResendMessageAsync(
                metadataAccessorMock.Object,
                registry,
                null!);

            // Assert
            metadataMock.Verify(m => m.CompleteMessageAsync(), Times.Once);
            messageSenderMock.Verify(
                s => s.SendMessageAsync(
                    It.IsAny<ServiceBusMessage>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
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
            metadataMock.Setup(m => m.CompleteMessageAsync()).Returns(Task.CompletedTask);

            var metadataAccessorMock = new Mock<IMessageMetadataAccessor>();
            metadataAccessorMock.Setup(a => a.Metadata).Returns(metadataMock.Object);

            // Create a mock for IMessageSender for the topic
            var messageSenderMock = new Mock<IMessageSender>();
            messageSenderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            messageSenderMock.Setup(s => s.ClientType).Returns(ClientType.Topic);
            messageSenderMock.Setup(s => s.Name).Returns(topicName);

            // Create registry with mocked dependencies
            var clientFactoryMock = new Mock<IClientFactory>();
            var optionsMock = new Mock<IOptions<ServiceBusOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new ServiceBusOptions());

            var registry = new ServiceBusRegistry(clientFactoryMock.Object, optionsMock.Object);

            // Register the IMessageSender mock with the registry
            var registerMethod = typeof(ServiceBusRegistry).GetMethod("Register",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                [typeof(IMessageSender)],
                null);

            registerMethod!.Invoke(registry, [messageSenderMock.Object]);

            // Act
            await messageContext.CompleteAndResendMessageAsync(
                metadataAccessorMock.Object,
                registry,
                null!);

            // Assert
            metadataMock.Verify(m => m.CompleteMessageAsync(), Times.Once);
            messageSenderMock.Verify(
                s => s.SendMessageAsync(
                    It.IsAny<ServiceBusMessage>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
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

            // Create registry with mocked dependencies
            var clientFactoryMock = new Mock<IClientFactory>();
            var optionsMock = new Mock<IOptions<ServiceBusOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new ServiceBusOptions());

            var registry = new ServiceBusRegistry(clientFactoryMock.Object, optionsMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                messageContext.CompleteAndResendMessageAsync(
                    metadataAccessorMock.Object,
                    registry,
                    null!));

            metadataMock.Verify(m => m.CompleteMessageAsync(), Times.Once);
            // No need to verify registry operations as the exception occurs first
        }
    }
}