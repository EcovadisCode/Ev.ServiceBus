using Azure.Core;
using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions.Configuration;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions;

public static class ClientOptionsExtensions
{
    /// <summary>
    /// Sets the connection to use for this resource.
    /// If no connection is set then the default connection will be used.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="connectionString"></param>
    /// <param name="connectionOptions"></param>
    /// <typeparam name="TOptions"></typeparam>
    /// <returns></returns>
    public static TOptions WithConnection<TOptions>(
        this TOptions options,
        string connectionString,
        ServiceBusClientOptions connectionOptions)
        where TOptions : ClientOptions
    {
        options.ConnectionSettings = new ConnectionSettings(connectionString, connectionOptions);
        return options;
    }

    /// <summary>
    /// Sets the connection to use for this resource using Azure Entra ID.
    /// If no connection is set then the default connection will be used.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="fullyQualifiedNamespace"></param>
    /// <param name="credentials"></param>
    /// <param name="connectionOptions"></param>
    /// <typeparam name="TOptions"></typeparam>
    /// <returns></returns>
    public static TOptions WithConnection<TOptions>(
        this TOptions options,
        string fullyQualifiedNamespace,
        TokenCredential credentials,
        ServiceBusClientOptions connectionOptions)
        where TOptions : ClientOptions
    {
        options.ConnectionSettings = new ConnectionSettings(fullyQualifiedNamespace, connectionOptions, credentials);
        return options;
    }
}