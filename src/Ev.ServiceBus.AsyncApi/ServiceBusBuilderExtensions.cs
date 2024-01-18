using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Saunter;

namespace Ev.ServiceBus.AsyncApi;

public static class ServiceBusBuilderExtensions
{
    public static ServiceBusBuilder PopulateAsyncApiSchemaWithEvServiceBus(this ServiceBusBuilder builder)
    {
        builder.Services.TryAddSingleton<DocumentFilter>();
        builder.Services.AddAsyncApiSchemaGeneration(
            options =>
            {
                if (options.DocumentFilters.Any(o => o == typeof(DocumentFilter)) == false)
                {
                    options.AddDocumentFilter<DocumentFilter>();
                }
            });
        return builder;
    }
}