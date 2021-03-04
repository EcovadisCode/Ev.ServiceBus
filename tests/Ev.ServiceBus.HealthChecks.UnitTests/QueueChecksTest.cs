using System.Linq;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ev.ServiceBus.HealthChecks.UnitTests
{
    public class QueueChecksTest
    {
        [Fact]
        public void QueueWithNoConnectionStringWillBeIgnored_case1()
        {
            var services = new ServiceCollection();

            services.AddServiceBus<PayloadParser>(settings =>
            {
            });

            services.AddHealthChecks().AddEvServiceBusChecks();

            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.RegisterDispatch<StudentCreated>();
            });

            var provider = services.BuildServiceProvider();

            var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

            healthOptions.Value.Registrations.Count.Should().Be(0);
        }

        [Fact]
        public void QueueWithNoConnectionStringWillBeIgnored_case2()
        {
            var services = new ServiceCollection();

            services.AddServiceBus<PayloadParser>(settings =>
            {
                settings.WithConnection(new ServiceBusConnectionStringBuilder());
            });

            services.AddHealthChecks().AddEvServiceBusChecks();

            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.RegisterDispatch<StudentCreated>();
            });

            var provider = services.BuildServiceProvider();

            var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

            healthOptions.Value.Registrations.Count.Should().Be(0);
        }

        [Fact]
        public void QueueWithNoConnectionStringWillBeIgnored_case3()
        {
            var services = new ServiceCollection();

            services.AddServiceBus<PayloadParser>(settings =>
            {
                settings.WithConnection(new ServiceBusConnection("Endpoint=sb://localhost.windows.net/;SharedAccessKeyName=accessKey;SharedAccessKey=6WXpAsTC+9QzmGiPt+58khMtryasgplsL6y9dpjSF1w="));
            });

            services.AddHealthChecks().AddEvServiceBusChecks();

            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.RegisterDispatch<StudentCreated>();
            });

            var provider = services.BuildServiceProvider();

            var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

            healthOptions.Value.Registrations.Count.Should().Be(0);
        }

        [Fact]
        public void QueueWithConnectionStringWillBeRegistered_case1()
        {
            var services = new ServiceCollection();

            services.AddServiceBus<PayloadParser>(settings =>
            {
                settings.WithConnection("testConnectionString");
            });

            services.AddHealthChecks().AddEvServiceBusChecks();

            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.RegisterDispatch<StudentCreated>();
            });

            var provider = services.BuildServiceProvider();

            var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

            healthOptions.Value.Registrations.Count.Should().Be(1);
            var reg = healthOptions.Value.Registrations.First();
            reg.Name.Should().Be("Queue:queue");
            reg.Tags.Should().Contain("Ev.ServiceBus");
        }

        [Fact]
        public void QueueWithConnectionStringWillBeRegistered_case2()
        {
            var services = new ServiceCollection();

            services.AddServiceBus<PayloadParser>(settings =>
            {
            });

            services.AddHealthChecks().AddEvServiceBusChecks();

            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.CustomizeConnection("testConnectionString");
                builder.RegisterDispatch<StudentCreated>();
            });

            var provider = services.BuildServiceProvider();

            var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

            healthOptions.Value.Registrations.Count.Should().Be(1);
            var reg = healthOptions.Value.Registrations.First();
            reg.Name.Should().Be("Queue:queue");
            reg.Tags.Should().Contain("Ev.ServiceBus");
        }

        [Fact]
        public void OnlyOneQueueWillBeRegistered_case1()
        {
            var services = new ServiceCollection();

            services.AddServiceBus<PayloadParser>(settings =>
            {
            });

            services.AddHealthChecks().AddEvServiceBusChecks();

            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.CustomizeConnection("testConnectionString");
                builder.RegisterDispatch<StudentCreated>();
            });
            services.RegisterServiceBusReception().FromQueue("queue", builder =>
            {
                builder.CustomizeConnection("testConnectionString");
                builder.RegisterReception<StudentCreated, StudentCreatedHandler>();
            });

            var provider = services.BuildServiceProvider();

            var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

            healthOptions.Value.Registrations.Count.Should().Be(1);
            var reg = healthOptions.Value.Registrations.First();
            reg.Name.Should().Be("Queue:queue");
            reg.Tags.Should().Contain("Ev.ServiceBus");
        }

        [Fact]
        public void OnlyOneQueueWillBeRegistered_case2()
        {
            var services = new ServiceCollection();

            services.AddServiceBus<PayloadParser>(settings =>
            {
            });

            services.AddHealthChecks().AddEvServiceBusChecks();

            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.CustomizeConnection("testConnectionString");
                builder.RegisterDispatch<StudentCreated>();
            });

            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.CustomizeConnection("testConnectionString");
                builder.RegisterDispatch<CourseCreated>();
            });

            var provider = services.BuildServiceProvider();

            var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

            healthOptions.Value.Registrations.Count.Should().Be(1);
            var reg = healthOptions.Value.Registrations.First();
            reg.Name.Should().Be("Queue:queue");
            reg.Tags.Should().Contain("Ev.ServiceBus");
        }
    }
}
