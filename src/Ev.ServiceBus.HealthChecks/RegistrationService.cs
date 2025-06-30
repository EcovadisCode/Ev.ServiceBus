using System.Linq;
using Ev.ServiceBus.Abstractions;
using HealthChecks.AzureServiceBus;
using HealthChecks.AzureServiceBus.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ev.ServiceBus.HealthChecks;

public class RegistrationService : IConfigureOptions<HealthCheckServiceOptions>
{
    private readonly ILogger<LoggingExtensions.HealthChecks> _logger;
    private readonly IOptions<ServiceBusOptions> _serviceBusOptions;

    public RegistrationService(IOptions<ServiceBusOptions> serviceBusOptions, ILogger<LoggingExtensions.HealthChecks> logger)
    {
        _serviceBusOptions = serviceBusOptions;
        _logger = logger;
    }

    public void Configure(HealthCheckServiceOptions options)
    {
        if (_serviceBusOptions.Value.Settings.Enabled == false)
        {
            return;
        }

        var commonConnectionString = _serviceBusOptions.Value.Settings.ConnectionSettings?.ConnectionString;
        var resources = _serviceBusOptions.Value.Receivers.Union(_serviceBusOptions.Value.Senders).Distinct()
            .ToArray();

        foreach (var resourceGroup in resources.GroupBy(o => o.ConnectionSettings, new ConnectionSettingsComparer()))
        {
            var connectionString = resourceGroup.Key?.ConnectionString ?? commonConnectionString;
            if (connectionString == null)
            {
                continue;
            }

            var queues = resourceGroup.Where(o => o is QueueOptions).Cast<QueueOptions>().GroupBy(o => o.QueueName.ToLower());
            foreach (var group in queues)
            {
                _logger.AddingHealthCheck("Queue", group.Key);
                options.Registrations.Add(new HealthCheckRegistration($"Queue:{group.Key}",
                    sp => (IHealthCheck) new AzureServiceBusQueueHealthCheck(new AzureServiceBusQueueHealthCheckOptions(group.Key)
                    {
                        ConnectionString = connectionString
                    }),
                    null, HealthChecksBuilderExtensions.HealthCheckTags, null));
            }

            var topics = resourceGroup.Where(o => o is TopicOptions).Cast<TopicOptions>().GroupBy(o => o.TopicName.ToLower());
            foreach (var group in topics)
            {
                _logger.AddingHealthCheck("Topic", group.Key);
                options.Registrations.Add(new HealthCheckRegistration($"Topic:{group.Key}",
                    sp => (IHealthCheck) new AzureServiceBusTopicHealthCheck(new AzureServiceBusTopicHealthCheckOptions(group.Key)
                    {
                        ConnectionString = connectionString
                    }),
                    null, HealthChecksBuilderExtensions.HealthCheckTags, null));
            }

            var subscriptions = resourceGroup
                .Where(o => o is SubscriptionOptions)
                .Cast<SubscriptionOptions>()
                .GroupBy(o => new { TopicName = o.TopicName.ToLower(), SubscriptionName = o.SubscriptionName.ToLower() });
            foreach (var group in subscriptions)
            {
                _logger.AddingHealthCheck("Subscription", $"{group.Key.TopicName}/Subscriptions/{group.Key.SubscriptionName}");
                options.Registrations.Add(new HealthCheckRegistration($"Subscription:{group.Key.TopicName}/Subscriptions/{group.Key.SubscriptionName}",
                    sp => (IHealthCheck) new AzureServiceBusSubscriptionHealthCheck(new AzureServiceBusSubscriptionHealthCheckHealthCheckOptions(group.Key.TopicName, group.Key.SubscriptionName)
                        {
                            ConnectionString = connectionString
                        }),
                    null, HealthChecksBuilderExtensions.HealthCheckTags, null));
            }
        }
    }
}