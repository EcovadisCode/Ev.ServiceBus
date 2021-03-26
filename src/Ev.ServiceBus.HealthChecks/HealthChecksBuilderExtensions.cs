using Ev.ServiceBus.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class HealthChecksBuilderExtensions
    {
        /// <summary>
        /// Add health checks for every registered resources in Ev.ServiceBus.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IHealthChecksBuilder AddEvServiceBusChecks(this IHealthChecksBuilder builder)
        {
            builder.Services.TryAddSingleton<IConfigureOptions<HealthCheckServiceOptions>, RegistrationService>();
            return builder;
        }
    }
}
