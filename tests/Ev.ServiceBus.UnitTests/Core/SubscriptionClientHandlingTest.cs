using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Xunit;

namespace Ev.ServiceBus.UnitTests.Core;

public class SubscriptionClientHandlingTest
{
    [Fact]
    public async Task ClosesTheSubscriptionClientsProperlyOnShutdown()
    {
        var composer = new Composer();

        composer.WithAdditionalServices(services =>
        {
            services.RegisterServiceBusReception().FromSubscription("testtopic1", "testsub1", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
                builder.RegisterReception<NoiseEvent, NoiseHandler>();
            });
            services.RegisterServiceBusReception().FromSubscription("testtopic2", "testsub1", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString2;", new ServiceBusClientOptions());
                builder.RegisterReception<NoiseEvent, NoiseHandler>();
            });
            services.RegisterServiceBusReception().FromSubscription("testtopic3", "testsub1", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString3;", new ServiceBusClientOptions());
                builder.RegisterReception<NoiseEvent, NoiseHandler>();
            });
        });

        var provider = await composer.Compose();

        await provider.SimulateStopHost(token: new CancellationToken());
        var clientMocks = composer.ClientFactory.GetAllProcessorMocks();

        clientMocks.All(o => o.IsClosed).Should().BeTrue();
    }
}