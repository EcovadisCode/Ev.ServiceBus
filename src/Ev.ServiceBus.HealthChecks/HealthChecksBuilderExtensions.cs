using System.Collections.Generic;
using Ev.ServiceBus.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class HealthChecksBuilderExtensions
{
    internal static readonly List<string> HealthCheckTags = new() {"Ev.ServiceBus"};

    /// <summary>
    /// Add health checks for every registered resources in Ev.ServiceBus.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="tags"></param>
    /// <returns></returns>
    public static IHealthChecksBuilder AddEvServiceBusChecks(
        this IHealthChecksBuilder builder,
        params string[] tags)
    {
        HealthCheckTags.AddRange(tags);
        builder.Services.AddSingleton<IConfigureOptions<HealthCheckServiceOptions>, RegistrationService>();
        return builder;
    }
}