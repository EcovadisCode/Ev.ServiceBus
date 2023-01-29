using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Batching;
using Ev.ServiceBus.Abstractions.Exceptions;
using Ev.ServiceBus.Management;

namespace Ev.ServiceBus.Batching;

public sealed class MessageBatcher : IMessageBatcher
{
    private const int MaxMessagePerSend = 100;
    private readonly IServiceBusRegistry _registry;
    private readonly IMessagePayloadSerializer _messagePayloadSerializer;
    private readonly ServiceBusRegistry _dispatchRegistry;

    public MessageBatcher(
        IServiceBusRegistry registry,
        IMessagePayloadSerializer messagePayloadSerializer,
        ServiceBusRegistry dispatchRegistry)
    {
        _registry = registry;
        _messagePayloadSerializer = messagePayloadSerializer;
        _dispatchRegistry = dispatchRegistry;
    }

    public async Task<IReadOnlyCollection<MessageBatch<T>>> CalculateBatches<T>(IEnumerable<T> payloads)
    {
        var batches = new List<MessageBatch<T>>();
        ServiceBusMessageBatch? serviceBusMessageBatch = default;

        try
        {
            foreach (var groupedMessages in CreateGroupedMessages(payloads).GroupBy(m => m.RegistrationKey))
            {
                var currentPayloads = new List<T>(MaxMessagePerSend);
                var sender = groupedMessages.Key.ClientType == ClientType.Queue
                    ? _registry.GetQueueSender(groupedMessages.Key.ResourceId)
                    : _registry.GetTopicSender(groupedMessages.Key.ResourceId);
                serviceBusMessageBatch = await sender.CreateMessageBatchAsync();
                foreach (var message in groupedMessages)
                {
                    if (FitsInBatch(serviceBusMessageBatch, message))
                    {
                        currentPayloads.Add(message.Payload);
                        continue;
                    }

                    var messageBatch = new MessageBatch<T>(currentPayloads);
                    batches.Add(messageBatch);
                    currentPayloads.Clear();
                    serviceBusMessageBatch.Dispose();
                    serviceBusMessageBatch = await sender.CreateMessageBatchAsync();
                    if (FitsInBatch(serviceBusMessageBatch, message))
                    {
                        currentPayloads.Add(message.Payload);
                        continue;
                    }

                    throw new BatchingFailedException();
                }

                if (currentPayloads.Any())
                {
                    var messageBatch = new MessageBatch<T>(currentPayloads);
                    batches.Add(messageBatch);
                    serviceBusMessageBatch.Dispose();
                }
            }
        }
        catch (BatchingFailedException)
        {
            serviceBusMessageBatch?.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            serviceBusMessageBatch?.Dispose();
            throw new BatchingFailedException(ex);
        }

        return batches;
    }

    private IEnumerable<PayloadWithServiceBusMessage<T>> CreateGroupedMessages<T>(IEnumerable<T> payloads)
    {
        return
            from payload in payloads
            let registrations = _dispatchRegistry.GetDispatchRegistrations(payload.GetType())
            from registration in registrations
            let message = CreateMessage(payload, registration)
            select new PayloadWithServiceBusMessage<T>(payload, message, registration);
    }

    private ServiceBusMessage CreateMessage<T>(T payload, MessageDispatchRegistration registration)
    {
        var result = _messagePayloadSerializer.SerializeBody(payload);
        var message = MessageHelper.CreateMessage(result.ContentType, result.Body, registration.PayloadTypeId);

        message.SessionId = Guid.NewGuid().ToString();
        message.CorrelationId = Guid.NewGuid().ToString();

        foreach (var customizer in registration.OutgoingMessageCustomizers)
        {
            customizer?.Invoke(message, payload);
        }

        return message;
    }

    private static bool FitsInBatch<T>(ServiceBusMessageBatch batch, PayloadWithServiceBusMessage<T> message)
    {
        return
            batch.Count < MaxMessagePerSend &&
            batch.TryAddMessage(message.ServiceBusMessage);
    }

    private sealed class PayloadWithServiceBusMessage<T>
    {
        public PayloadWithServiceBusMessage(T payload, ServiceBusMessage serviceBusMessage, MessageDispatchRegistration registration)
        {
            Payload = payload;
            ServiceBusMessage = serviceBusMessage;
            RegistrationKey = (registration.Options.ClientType, registration.Options.ResourceId);
        }

        public T Payload { get; }
        public ServiceBusMessage ServiceBusMessage { get; }
        public (ClientType ClientType, string ResourceId) RegistrationKey { get; }
    }
}