using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Ev.ServiceBus.UnitTests.Helpers;

public class Composer : IDisposable
{
    private readonly List<Action<IServiceCollection>> _additionalServices;
    private Action<IServiceCollection> _overrideFactory;
    private Action<ServiceBusSettings> _defaultSettings;
    private Action<ServiceBusBuilder> _additionalOptions;

    public Composer()
    {
        _additionalServices = new List<Action<IServiceCollection>>(5);
        _overrideFactory = s => s.OverrideClientFactory();
        _defaultSettings = settings => { settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions()); };
        _additionalOptions = _ => { };
    }

    private readonly List<KeyValuePair<SenderType, string>> _listOfDispatchSenders = new List<KeyValuePair<SenderType, string>>(5);

    public ServiceProvider Provider { get; private set; }
    public FakeClientFactory ClientFactory { get; private set; }

    public void Dispose()
    {
        if (Provider != null)
        {
            Provider.SimulateStopHost(default).GetAwaiter().GetResult();
            Provider.Dispose();
            Provider = null;
        }
    }

    public void OverrideClientFactory(IClientFactory factory)
    {
        _overrideFactory = s => s.OverrideClientFactory(factory);
    }

    public Composer WithAdditionalServices(Action<IServiceCollection> action)
    {
        _additionalServices.Add(action);
        return this;
    }

    public void WithDispatchQueueSender(string queueName)
    {
        _listOfDispatchSenders.Add(new KeyValuePair<SenderType, string>(SenderType.Queue, queueName));
    }

    public void WithDefaultSettings(Action<ServiceBusSettings> defaultSettings)
    {
        _defaultSettings = defaultSettings;
    }

    public void WithAdditionalOptions(Action<ServiceBusBuilder> options)
    {
        _additionalOptions = options;
    }

    private void ComposeSenders(IServiceCollection services)
    {
        if (_listOfDispatchSenders.Count == 0)
        {
            return;
        }

        var serviceBusRegistry = new Mock<IServiceBusRegistry>();
        foreach (var sender in _listOfDispatchSenders)
        {
            var messageSender = new Mock<IMessageSender>();
            messageSender.Setup(o => o.CreateMessageBatchAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ServiceBusModelFactory.ServiceBusMessageBatch(0, new List<ServiceBusMessage>()));
            switch (sender.Key)
            {
                case SenderType.Queue:
                    serviceBusRegistry.Setup(s => s.GetQueueSender(sender.Value))
                        .Returns(messageSender.Object);
                    break;
                case SenderType.Topic:
                    serviceBusRegistry.Setup(s => s.GetTopicSender(sender.Value))
                        .Returns(messageSender.Object);
                    break;
            }
        }
        services.AddSingleton(s => serviceBusRegistry.Object);
    }

    public async Task<IServiceProvider> Compose()
    {
        var services = new ServiceCollection();

        var builder = services.AddServiceBus<PayloadSerializer>(_defaultSettings);
        _additionalOptions(builder);

        ComposeSenders(services);

        _overrideFactory(services);
        _additionalServices.ForEach(a => a?.Invoke(services));

        Provider = services.BuildServiceProvider();
        await Provider.SimulateStartHost(new CancellationToken());

        ClientFactory = Provider.GetService<FakeClientFactory>();
        return Provider;
    }



    enum SenderType
    {
        Queue,
        Topic
    }
}