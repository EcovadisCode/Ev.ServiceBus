using System.Linq;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
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

            services.AddServiceBus<PayloadSerializer>(settings =>
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
        public void QueueWithConnectionStringWillBeRegistered_case1()
        {
            var services = new ServiceCollection();

            services.AddServiceBus<PayloadSerializer>(settings =>
            {
                settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
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

            services.AddServiceBus<PayloadSerializer>(settings =>
            {
            });

            services.AddHealthChecks().AddEvServiceBusChecks();

            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
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

            services.AddServiceBus<PayloadSerializer>(settings =>
            {
            });

            services.AddHealthChecks().AddEvServiceBusChecks();

            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
                builder.RegisterDispatch<StudentCreated>();
            });
            services.RegisterServiceBusReception().FromQueue("queue", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
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

            services.AddServiceBus<PayloadSerializer>(settings =>
            {
            });

            services.AddHealthChecks().AddEvServiceBusChecks();

            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
                builder.RegisterDispatch<StudentCreated>();
            });

            services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
            {
                builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
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
