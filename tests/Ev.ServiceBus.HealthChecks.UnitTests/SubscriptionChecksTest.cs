using System.Linq;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ev.ServiceBus.HealthChecks.UnitTests;

public class SubscriptionChecksTest
{
    [Fact]
    public void SubscriptionWithNoConnectionStringWillBeIgnored_case1()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
        });

        services.AddHealthChecks().AddEvServiceBusChecks();

        services.RegisterServiceBusReception().FromSubscription("topic", "subscription", builder =>
        {
            builder.RegisterReception<StudentCreated, StudentCreatedHandler>();
        });

        var provider = services.BuildServiceProvider();

        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        healthOptions.Value.Registrations.Count.Should().Be(0);
    }

    [Fact]
    public void SubscriptionWithConnectionStringWillBeRegistered_case1()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
            settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
        });

        services.AddHealthChecks().AddEvServiceBusChecks();

        services.RegisterServiceBusReception().FromSubscription("topic", "subscription", builder =>
        {
            builder.RegisterReception<StudentCreated, StudentCreatedHandler>();
        });

        var provider = services.BuildServiceProvider();

        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        healthOptions.Value.Registrations.Count.Should().Be(1);
        var reg = healthOptions.Value.Registrations.First();
        reg.Name.Should().Be("Subscription:topic/Subscriptions/subscription");
        reg.Tags.Should().Contain("Ev.ServiceBus");
    }

    [Fact]
    public void SubscriptionWithConnectionStringWillBeRegistered_case2()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
        });

        services.AddHealthChecks().AddEvServiceBusChecks();

        services.RegisterServiceBusReception().FromSubscription("topic", "subscription", builder =>
        {
            builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
            builder.RegisterReception<CourseCreated, CourseCreatedHandler>();
        });

        var provider = services.BuildServiceProvider();

        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        healthOptions.Value.Registrations.Count.Should().Be(1);
        var reg = healthOptions.Value.Registrations.First();
        reg.Name.Should().Be("Subscription:topic/Subscriptions/subscription");
        reg.Tags.Should().Contain("Ev.ServiceBus");
    }

    [Fact]
    public void OnlyOneSubscriptionWillBeRegistered_case1()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
            settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
        });

        services.AddHealthChecks().AddEvServiceBusChecks();

        services.RegisterServiceBusReception().FromSubscription("topic", "subscription", builder =>
        {
            builder.RegisterReception<CourseCreated, CourseCreatedHandler>();
        });
        services.RegisterServiceBusReception().FromSubscription("topic", "subscription", builder =>
        {
            builder.RegisterReception<StudentCreated, StudentCreatedHandler>();
        });

        var provider = services.BuildServiceProvider();

        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        healthOptions.Value.Registrations.Count.Should().Be(1);
        var reg = healthOptions.Value.Registrations.First();
        reg.Name.Should().Be("Subscription:topic/Subscriptions/subscription");
        reg.Tags.Should().Contain("Ev.ServiceBus");
    }
}