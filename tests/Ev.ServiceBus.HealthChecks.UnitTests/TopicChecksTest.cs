using System.Linq;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ev.ServiceBus.HealthChecks.UnitTests;

public class TopicChecksTest
{
    private const string TagOne = nameof(TagOne);
    private const string TagTwo = nameof(TagTwo);

    [Fact]
    public void TopicWithNoConnectionStringWillBeIgnored_case1()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
        });

        services.AddHealthChecks().AddEvServiceBusChecks();

        services.RegisterServiceBusDispatch().ToTopic("topic", builder =>
        {
            builder.RegisterDispatch<StudentCreated>();
        });

        var provider = services.BuildServiceProvider();

        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        healthOptions.Value.Registrations.Count.Should().Be(0);
    }

    [Fact]
    public void TopicWithConnectionStringWillBeRegistered_case1()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
            settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
        });

        services.AddHealthChecks().AddEvServiceBusChecks(TagOne, TagTwo);

        services.RegisterServiceBusDispatch().ToTopic("topic", builder =>
        {
            builder.RegisterDispatch<StudentCreated>();
        });

        var provider = services.BuildServiceProvider();

        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        healthOptions.Value.Registrations.Count.Should().Be(1);
        var reg = healthOptions.Value.Registrations.First();
        reg.Name.Should().Be("Topic:topic");
        reg.Tags.Should().HaveCount(3);
        reg.Tags.Should().Contain("Ev.ServiceBus");
        reg.Tags.Should().Contain(TagOne);
        reg.Tags.Should().Contain(TagTwo);
    }

    [Fact]
    public void TopicWithConnectionStringWillBeRegistered_case2()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
        });

        services.AddHealthChecks().AddEvServiceBusChecks(TagOne, TagTwo);

        services.RegisterServiceBusDispatch().ToTopic("topic", builder =>
        {
            builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
            builder.RegisterDispatch<StudentCreated>();
        });

        var provider = services.BuildServiceProvider();

        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        healthOptions.Value.Registrations.Count.Should().Be(1);
        var reg = healthOptions.Value.Registrations.First();
        reg.Name.Should().Be("Topic:topic");
        reg.Tags.Should().HaveCount(3);
        reg.Tags.Should().Contain("Ev.ServiceBus");
        reg.Tags.Should().Contain(TagOne);
        reg.Tags.Should().Contain(TagTwo);
    }

    [Fact]
    public void OnlyOneTopicWillBeRegistered_case1()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
        });

        services.AddHealthChecks().AddEvServiceBusChecks(TagOne, TagTwo);

        services.RegisterServiceBusDispatch().ToTopic("topic", builder =>
        {
            builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
            builder.RegisterDispatch<StudentCreated>();
        });

        services.RegisterServiceBusDispatch().ToTopic("topic", builder =>
        {
            builder.CustomizeConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
            builder.RegisterDispatch<CourseCreated>();
        });

        var provider = services.BuildServiceProvider();

        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        healthOptions.Value.Registrations.Count.Should().Be(1);
        var reg = healthOptions.Value.Registrations.First();
        reg.Name.Should().Be("Topic:topic");
        reg.Tags.Should().HaveCount(3);
        reg.Tags.Should().Contain("Ev.ServiceBus");
        reg.Tags.Should().Contain(TagOne);
        reg.Tags.Should().Contain(TagTwo);
    }
}