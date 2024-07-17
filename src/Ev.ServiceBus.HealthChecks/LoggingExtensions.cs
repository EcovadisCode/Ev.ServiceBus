using System;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.HealthChecks;

public static class LoggingExtensions
{
    public record HealthChecks();

    private static readonly Action<ILogger, string, string, Exception?> LogAddingHealthCheck =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, nameof(AddingHealthCheck)),
            "Adding health check for {EVSB_Client} {EVSB_ResourceId}"
        );

    public static void AddingHealthCheck(this ILogger logger, string resourceId, string client)
        => LogAddingHealthCheck(logger,client, resourceId, default);

}