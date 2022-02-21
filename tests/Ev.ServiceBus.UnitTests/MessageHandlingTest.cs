using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class MessageHandlingTest
    {
        [Fact]
        public async Task AScopeIsCreatedForEachMessageReceived()
        {
            var composer = new Composer();

            composer.WithAdditionalServices(
                services =>
                {
                    services.AddSingleton<InstanceRegistry>();
                    services.AddSingleton<SingletonObject>();
                    services.AddScoped<ScopedObject>();
                    services.AddTransient<TransientObject>();

                    services.RegisterServiceBusQueue("testQueue")
                        .WithConnection("Endpoint=connectionStringTest;", new ServiceBusClientOptions())
                        .WithCustomMessageHandler<InstanceCounterMessageHandler>(_ => {});
                });

            var provider = await composer.Compose();

            var clientMock = composer.ClientFactory.GetProcessorMock("testQueue");

            await clientMock.TriggerMessageReception(new ServiceBusMessage(), new CancellationToken());
            await clientMock.TriggerMessageReception(new ServiceBusMessage(), new CancellationToken());

            var registry = provider.GetService<InstanceRegistry>();

            var numberOfSingletonInstances =
                registry.Instances.Where(o => o is SingletonObject).GroupBy(o => o).Count();
            var numberOfScopedInstances = registry.Instances.Where(o => o is ScopedObject).GroupBy(o => o).Count();
            var numberOfTransientInstances =
                registry.Instances.Where(o => o is TransientObject).GroupBy(o => o).Count();

            Assert.Equal(1, numberOfSingletonInstances);
            Assert.Equal(2, numberOfScopedInstances);
            Assert.Equal(4, numberOfTransientInstances);
        }

        [Fact]
        public async Task CustomExceptionHandlerIsCalledWhenExceptionOccurs()
        {
            var composer = new Composer();

            var mock = new Mock<IMessageHandler>();

            var exceptionMock = new Mock<IExceptionHandler>();
            exceptionMock.Setup(o => o.HandleExceptionAsync(It.IsAny<ProcessErrorEventArgs>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            composer.WithAdditionalServices(
                services =>
                {
                    services.AddSingleton(mock);
                    services.AddSingleton(exceptionMock);

                    var queue = services.RegisterServiceBusQueue("testQueue")
                        .WithConnection("Endpoint=connectionStringTest;", new ServiceBusClientOptions());
                    queue.WithCustomMessageHandler<FakeMessageHandler>(_ => {});
                    queue.WithCustomExceptionHandler<FakeExceptionHandler>();
                });

            var provider = await composer.Compose();

            var clientMock = provider.GetProcessorMock("testQueue");

            var sentArgs = new ProcessErrorEventArgs(new Exception(), ServiceBusErrorSource.Abandon, "", "", CancellationToken.None);
            await clientMock.TriggerExceptionOccured(sentArgs);

            exceptionMock.Verify(
                o => o.HandleExceptionAsync(sentArgs),
                Times.Once);
        }

        [Fact]
        public async Task WontFailWhenNoCustomExceptionHandlerIsSetAndExceptionOccurs()
        {
            var composer = new Composer();

            var mock = new Mock<IMessageHandler>();

            composer.WithAdditionalServices(
                services =>
                {
                    services.AddSingleton(mock);

                    services.RegisterServiceBusQueue("testQueue")
                        .WithConnection("Endpoint=connectionStringTest;", new ServiceBusClientOptions())
                        .WithCustomMessageHandler<FakeMessageHandler>(_ => {});
                });

            var provider = await composer.Compose();

            var clientMock = provider.GetProcessorMock("testQueue");

            var sentArgs = new ProcessErrorEventArgs(new Exception(), ServiceBusErrorSource.Abandon, "", "", CancellationToken.None);
            await clientMock.TriggerExceptionOccured(sentArgs);
        }

        private class TransientObject
        {
        }

        private class ScopedObject
        {
        }

        private class SingletonObject
        {
        }

        private class InstanceRegistry
        {
            public List<object> Instances { get; } = new List<object>();
        }

        private class InstanceCounterMessageHandler : IMessageHandler
        {
            private readonly IServiceProvider _provider;

            public InstanceCounterMessageHandler(IServiceProvider provider)
            {
                _provider = provider;
            }

            public Task HandleMessageAsync(MessageContext context)
            {
                var registry = _provider.GetService<InstanceRegistry>();
                registry.Instances.Add(_provider.GetService<TransientObject>());
                registry.Instances.Add(_provider.GetService<TransientObject>());
                registry.Instances.Add(_provider.GetService<ScopedObject>());
                registry.Instances.Add(_provider.GetService<ScopedObject>());
                registry.Instances.Add(_provider.GetService<SingletonObject>());
                registry.Instances.Add(_provider.GetService<SingletonObject>());
                return Task.CompletedTask;
            }
        }
    }
}
