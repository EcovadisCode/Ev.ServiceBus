using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public static class ClientOptionsExtensions
    {
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

        public static TOptions WithConnection<TOptions>(
            this TOptions options,
            ServiceBusConnectionStringBuilder connectionStringBuilder,
            ReceiveMode receiveMode = ReceiveMode.PeekLock,
            RetryPolicy? retryPolicy = null)
            where TOptions : ClientOptions
        {
            connectionStringBuilder.EntityPath = options.EntityPath;
            options.ConnectionSettings = new ConnectionSettings(connectionStringBuilder, receiveMode, retryPolicy);
            return options;
        }
    }
}
