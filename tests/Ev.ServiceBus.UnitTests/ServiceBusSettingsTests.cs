using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
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
                    settings.WithConnection("testConnectionString");
                });
            var provider = await composer.Compose();

            var options = provider.GetService<IOptions<ServiceBusOptions>>();

            options.Value.Settings.ConnectionSettings.Should().NotBeNull();
            options.Value.Settings.ConnectionSettings!.ConnectionString.Should().Be("testConnectionString");
            options.Value.Settings.ConnectionSettings!.Connection.Should().BeNull();
            options.Value.Settings.ConnectionSettings!.ConnectionStringBuilder.Should().BeNull();
        }

        [Fact]
        public async Task ServiceBusSettingsStateAfterCallOfWithConnection_Connection()
        {
            var composer = new Composer();

            var connection = new ServiceBusConnection("Endpoint=sb://labepdvsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=TOEhvlmrOoLjHfxhYJ3xjoLtVZrMQLqP8MUwrv5flOA=");
            composer.WithDefaultSettings(
                settings =>
                {
                    settings.WithConnection(connection);
                });
            var provider = await composer.Compose();

            var options = provider.GetService<IOptions<ServiceBusOptions>>();

            options.Value.Settings.ConnectionSettings.Should().NotBeNull();
            options.Value.Settings.ConnectionSettings!.ConnectionString.Should().BeNull();
            options.Value.Settings.ConnectionSettings!.Connection.Should().Be(connection);
            options.Value.Settings.ConnectionSettings!.ConnectionStringBuilder.Should().BeNull();
        }

        [Fact]
        public async Task ServiceBusSettingsStateAfterCallOfWithConnection_ConnectionStringBuilder()
        {
            var composer = new Composer();

            var builder = new ServiceBusConnectionStringBuilder();
            composer.WithDefaultSettings(
                settings =>
                {
                    settings.WithConnection(builder);
                });
            var provider = await composer.Compose();

            var options = provider.GetService<IOptions<ServiceBusOptions>>();

            options.Value.Settings.ConnectionSettings.Should().NotBeNull();
            options.Value.Settings.ConnectionSettings!.ConnectionString.Should().BeNull();
            options.Value.Settings.ConnectionSettings!.Connection.Should().BeNull();
            options.Value.Settings.ConnectionSettings!.ConnectionStringBuilder.Should().Be(builder);
        }

        [Theory]
        [InlineData("connectionString", "connection")]
        [InlineData("connectionString", "connectionStringBuilder")]
        [InlineData("connection", "connectionString")]
        [InlineData("connection", "connectionStringBuilder")]
        [InlineData("connectionStringBuilder", "connectionString")]
        [InlineData("connectionStringBuilder", "connection")]
        public async Task SubsequentCallOfWithConnectionOverridesConnectionSettings(string case1, string case2)
        {
            var composer = new Composer();

            var builder = new ServiceBusConnectionStringBuilder();
            var connection = new ServiceBusConnection("Endpoint=sb://labepdvsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=TOEhvlmrOoLjHfxhYJ3xjoLtVZrMQLqP8MUwrv5flOA=");
            composer.WithDefaultSettings(
                settings =>
                {
                    switch (case1)
                    {
                        case "connectionString": settings.WithConnection("testConnectionString");
                            break;
                        case "connection": settings.WithConnection(connection);
                            break;
                        case "connectionStringBuilder": settings.WithConnection(builder);
                            break;
                    }
                    switch (case2)
                    {
                        case "connectionString": settings.WithConnection("testConnectionString");
                            break;
                        case "connection": settings.WithConnection(connection);
                            break;
                        case "connectionStringBuilder": settings.WithConnection(builder);
                            break;
                    }
                });
            var provider = await composer.Compose();

            var options = provider.GetService<IOptions<ServiceBusOptions>>();

            options.Value.Settings.ConnectionSettings.Should().NotBeNull();
            switch (case2)
            {
                case "connectionString":
                {
                    options.Value.Settings.ConnectionSettings!.ConnectionString.Should().Be("testConnectionString");
                    options.Value.Settings.ConnectionSettings!.Connection.Should().BeNull();
                    options.Value.Settings.ConnectionSettings!.ConnectionStringBuilder.Should().BeNull();
                }
                    break;
                case "connection":
                {
                    options.Value.Settings.ConnectionSettings!.ConnectionString.Should().BeNull();
                    options.Value.Settings.ConnectionSettings!.Connection.Should().Be(connection);
                    options.Value.Settings.ConnectionSettings!.ConnectionStringBuilder.Should().BeNull();
                }
                    break;
                case "connectionStringBuilder":
                {
                    options.Value.Settings.ConnectionSettings!.ConnectionString.Should().BeNull();
                    options.Value.Settings.ConnectionSettings!.Connection.Should().BeNull();
                    options.Value.Settings.ConnectionSettings!.ConnectionStringBuilder.Should().Be(builder);
                }
                    break;
            }
        }

    }
}
