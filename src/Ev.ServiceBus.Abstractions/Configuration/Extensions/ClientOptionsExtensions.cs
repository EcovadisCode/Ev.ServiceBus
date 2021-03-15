using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public static class ClientOptionsExtensions
    {
        /// <summary>
        /// Sets the connection to use for this resource.
        /// If no connection is set then the default connection will be used.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="connectionString"></param>
        /// <param name="receiveMode"></param>
        /// <param name="retryPolicy"></param>
        /// <typeparam name="TOptions"></typeparam>
        /// <returns></returns>
        public static TOptions WithConnection<TOptions>(
            this TOptions options,
            string connectionString,
            ReceiveMode receiveMode = ReceiveMode.PeekLock,
            RetryPolicy? retryPolicy = null)
            where TOptions : ClientOptions
        {
            options.ConnectionSettings = new ConnectionSettings(connectionString, receiveMode, retryPolicy);
            return options;
        }

        /// <summary>
        /// Sets the connection to use for this resource.
        /// If no connection is set then the default connection will be used.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="connection"></param>
        /// <param name="receiveMode"></param>
        /// <param name="retryPolicy"></param>
        /// <typeparam name="TOptions"></typeparam>
        /// <returns></returns>
        public static TOptions WithConnection<TOptions>(
            this TOptions options,
            ServiceBusConnection connection,
            ReceiveMode receiveMode = ReceiveMode.PeekLock,
            RetryPolicy? retryPolicy = null)
            where TOptions : ClientOptions
        {
            options.ConnectionSettings = new ConnectionSettings(connection, receiveMode, retryPolicy);
            return options;
        }

        /// <summary>
        /// Sets the connection to use for this resource.
        /// If no connection is set then the default connection will be used.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="connectionStringBuilder"></param>
        /// <param name="receiveMode"></param>
        /// <param name="retryPolicy"></param>
        /// <typeparam name="TOptions"></typeparam>
        /// <returns></returns>
        public static TOptions WithConnection<TOptions>(
            this TOptions options,
            ServiceBusConnectionStringBuilder connectionStringBuilder,
            ReceiveMode receiveMode = ReceiveMode.PeekLock,
            RetryPolicy? retryPolicy = null)
            where TOptions : ClientOptions
        {
            connectionStringBuilder.EntityPath = options.ResourceId;
            options.ConnectionSettings = new ConnectionSettings(connectionStringBuilder, receiveMode, retryPolicy);
            return options;
        }
    }
}
