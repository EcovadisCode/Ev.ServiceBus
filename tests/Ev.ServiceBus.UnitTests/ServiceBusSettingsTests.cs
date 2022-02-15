using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ev.ServiceBus.UnitTests
{
    public class ServiceBusSettingsTests
    {
        [Fact]
        public async Task ServiceBusSettingsStateByDefault()
        {
            var composer = new Composer();

            composer.WithDefaultSettings(settings => { });
            var provider = await composer.Compose();

            var options = provider.GetService<IOptions<ServiceBusOptions>>();

            options.Value.Settings.Enabled.Should().Be(true);
            options.Value.Settings.ReceiveMessages.Should().Be(true);
            options.Value.Settings.ConnectionSettings.Should().BeNull();
        }

        [Fact]
        public async Task ServiceBusSettingsStateAfterCallOfWithConnection_string()
        {
            var composer = new Composer();

            composer.WithDefaultSettings(
                settings =>
                {
                    settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
                });
            var provider = await composer.Compose();

            var options = provider.GetService<IOptions<ServiceBusOptions>>();

            options.Value.Settings.ConnectionSettings.Should().NotBeNull();
            options.Value.Settings.ConnectionSettings!.Endpoint.Should().Be("testConnectionString");
        }
    }
}
