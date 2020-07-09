using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public static class ClientOptionsExtensions
    {
        public static TOptions WithConnectionString<TOptions>(this TOptions options, string connectionString)
            where TOptions : ClientOptions
        {
            options.ConnectionString = connectionString;
            return options;
        }

        public static TOptions WithConnection<TOptions>(this TOptions options, ServiceBusConnection connection)
            where TOptions : ClientOptions
        {
            options.Connection = connection;
            return options;
        }

        public static TOptions WithConnectionStringBuilder<TOptions>(
            this TOptions options,
            ServiceBusConnectionStringBuilder connectionStringBuilder) where TOptions : ClientOptions
        {
            connectionStringBuilder.EntityPath = options.EntityPath;
            options.ConnectionStringBuilder = connectionStringBuilder;
            return options;
        }

        public static TOptions WithReceiveMode<TOptions>(this TOptions options, ReceiveMode receiveMode)
            where TOptions : ClientOptions
        {
            options.ReceiveMode = receiveMode;
            return options;
        }

        public static TOptions WithRetryPolicy<TOptions>(this TOptions options, RetryPolicy retryPolicy)
            where TOptions : ClientOptions
        {
            options.RetryPolicy = retryPolicy;
            return options;
        }
    }
}
