using Azure.Messaging.ServiceBus.Administration;
using System.Threading.Tasks;

namespace Ev.ServiceBus;

public static class ServiceBusIsolationExtensions
{
    public static string InstanceSuffix { get; internal set; }

    public static async Task CreateSubscription(string connectionString, string topic, string subscriptionName)
    {
        var adminClient = new ServiceBusAdministrationClient(connectionString);

        if (!await adminClient.SubscriptionExistsAsync(topic, subscriptionName))
        {
            var createOptions = new CreateSubscriptionOptions(topic, subscriptionName);
            var filter = new SqlRuleFilter($"IsolationKey = '{subscriptionName}'");
            var ruleOptions = new CreateRuleOptions("IsolationKeyFilter", filter);
            await adminClient.CreateSubscriptionAsync(createOptions, ruleOptions);
        }
    }

    public static string GetInstanceSubscriptionName(string subscriptionName)
    {
        return $"{subscriptionName}-{InstanceSuffix}";
    }
}
