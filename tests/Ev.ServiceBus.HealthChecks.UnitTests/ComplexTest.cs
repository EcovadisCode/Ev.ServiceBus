using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ev.ServiceBus.HealthChecks.UnitTests;

public class ComplexTest
{
    [Fact]
    public void MultipleRegistrationsGoesWell_case1()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
            settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
        });

        services.AddHealthChecks().AddEvServiceBusChecks();

        services.RegisterServiceBusDispatch().ToQueue("queue", builder =>
        {
            builder.RegisterDispatch<RelationCreated>();
        });
        services.RegisterServiceBusReception().FromQueue("queue", builder =>
        {
            builder.RegisterReception<RelationCreated, RelationCreatedHandler>();
        });
        services.RegisterServiceBusDispatch().ToTopic("topic", builder =>
        {
            builder.RegisterDispatch<CourseCreated>();
            builder.RegisterDispatch<StudentCreated>();
        });
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

        healthOptions.Value.Registrations.Count.Should().Be(3);
        healthOptions.Value.Registrations.Should().SatisfyRespectively(
            reg => { reg.Name.Should().Be("Queue:queue"); },
            reg => { reg.Name.Should().Be("Topic:topic"); },
            reg => { reg.Name.Should().Be("Subscription:topic/Subscriptions/subscription"); });
    }

    [Fact]
    public void MultipleRegistrationsGoesWell_case2()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
            settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
        });

        services.AddHealthChecks().AddEvServiceBusChecks();

        services.RegisterServiceBusDispatch().ToTopic("topic1", builder =>
        {
            builder.RegisterDispatch<RelationCreated>();
        });
        services.RegisterServiceBusDispatch().ToTopic("topic2", builder =>
        {
            builder.RegisterDispatch<StudentCreated>();
        });
        services.RegisterServiceBusDispatch().ToTopic("topic3", builder =>
        {
            builder.RegisterDispatch<CourseCreated>();
        });

        var provider = services.BuildServiceProvider();

        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        healthOptions.Value.Registrations.Count.Should().Be(3);
        healthOptions.Value.Registrations.Should().SatisfyRespectively(
            reg => { reg.Name.Should().Be("Topic:topic1"); },
            reg => { reg.Name.Should().Be("Topic:topic2"); },
            reg => { reg.Name.Should().Be("Topic:topic3"); });
    }

    [Fact]
    public void MultipleRegistrationsGoesWell_case3()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
            settings.WithConnection("Endpoint=testConnectionString;", new ServiceBusClientOptions());
        });

        services.AddHealthChecks().AddEvServiceBusChecks();

        services.RegisterServiceBusDispatch().ToQueue("queue1", builder =>
        {
            builder.RegisterDispatch<RelationCreated>();
        });
        services.RegisterServiceBusDispatch().ToQueue("queue2", builder =>
        {
            builder.RegisterDispatch<StudentCreated>();
        });
        services.RegisterServiceBusDispatch().ToQueue("queue3", builder =>
        {
            builder.RegisterDispatch<CourseCreated>();
        });

        var provider = services.BuildServiceProvider();

        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        healthOptions.Value.Registrations.Count.Should().Be(3);
        healthOptions.Value.Registrations.Should().SatisfyRespectively(
            reg => { reg.Name.Should().Be("Queue:queue1"); },
            reg => { reg.Name.Should().Be("Queue:queue2"); },
            reg => { reg.Name.Should().Be("Queue:queue3"); });
    }

    [Fact]
    public void MultipleRegistrationsGoesWell_case4()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
            settings.WithConnection("Endpoint=acmecompany.servicebus.windows.net;", new ServiceBusClientOptions());
        });

        services.AddHealthChecks().AddEvServiceBusChecks();

        services.RegisterServiceBusReception().FromSubscription("topic", "subscription1", builder =>
        {
            builder.RegisterReception<RelationCreated, RelationCreatedHandler>();
        });
        services.RegisterServiceBusReception().FromSubscription("topic", "subscription2", builder =>
        {
            builder.RegisterReception<CourseCreated, CourseCreatedHandler>();
        });
        services.RegisterServiceBusReception().FromSubscription("topic", "subscription3", builder =>
        {
            builder.RegisterReception<StudentCreated, StudentCreatedHandler>();
        });

        var provider = services.BuildServiceProvider();

        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        healthOptions.Value.Registrations.Count.Should().Be(3);
        healthOptions.Value.Registrations.Should().SatisfyRespectively(
            reg => { reg.Name.Should().Be("Subscription:topic/Subscriptions/subscription1"); },
            reg => { reg.Name.Should().Be("Subscription:topic/Subscriptions/subscription2"); },
            reg => { reg.Name.Should().Be("Subscription:topic/Subscriptions/subscription3"); });
    }

    [Fact]
    public void HealthCheckWorksWithEntraAuthorization()
    {
        var services = new ServiceCollection();

        services.AddServiceBus(settings =>
        {
            settings.WithConnection("acmecompany.servicebus.windows.net", new DefaultAzureCredential(), new ServiceBusClientOptions());
        });

        services.AddHealthChecks().AddEvServiceBusChecks();

        services.RegisterServiceBusReception().FromSubscription("topic", "subscription", builder =>
        {
            builder.RegisterReception<RelationCreated, RelationCreatedHandler>();
        });
        services.RegisterServiceBusReception().FromQueue("queue", builder =>
        {
            builder.RegisterReception<RelationCreated, RelationCreatedHandler>();
        });

        var provider = services.BuildServiceProvider();

        var healthOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>();

        healthOptions.Value.Registrations.Count.Should().Be(2);
        healthOptions.Value.Registrations.Should().SatisfyRespectively(
            reg => { reg.Name.Should().Be("Queue:queue"); },
            reg => { reg.Name.Should().Be("Subscription:topic/Subscriptions/subscription"); }
        );
    }

    public class RelationCreated { }

    public class RelationCreatedHandler : IMessageReceptionHandler<RelationCreated>
    {
        public Task Handle(RelationCreated @event, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}