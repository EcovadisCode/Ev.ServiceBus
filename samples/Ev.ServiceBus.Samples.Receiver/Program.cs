using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.AsyncApi;
using Ev.ServiceBus.Prometheus;
using Ev.ServiceBus.Sample.Contracts;
using Ev.ServiceBus.Samples.Receiver.ServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Saunter;
using Saunter.AsyncApiSchema.v2;

namespace Ev.ServiceBus.Samples.Receiver;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = CreateHostBuilder(args);
        var app = builder.Build();

        if (builder.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseMetricServer();
        app.UseHttpMetrics();
        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllers();
        app.MapAsyncApiUi();
        app.MapAsyncApiDocuments();
        app.Run();
    }

    private static WebApplicationBuilder CreateHostBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSwaggerGen();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "hh:mm:ss ";
        });
        builder.Services.AddHostedService<ServicebusPrometheusExporter>();
        builder.Services.AddControllers();

        builder.Services.AddAsyncApiSchemaGeneration(
            options =>
            {
                options.AsyncApi = new AsyncApiDocument()
                {
                    Info = new Info("Receiver API", "1.0.0")
                    {
                        Description = "Sample receiver project",
                    }
                };
            });

        builder.Services.AddServiceBus(
                settings =>
                {
                    // Provide a connection string here !
                    settings.WithConnection(
                        "Endpoint=sb://yourconnection.servicebus.windows.net/;SharedAccessKeyName=yourkeyh;SharedAccessKey=ae6pTuOBAFDH2y7xJJf9BFubZGxXMToN6B9NiVgLnbQ=",
                        new ServiceBusClientOptions());

                    // 
                    settings.WithIsolation(
                        IsolationBehavior.HandleIsolatedMessages,
                        "MyIsolationKey",
                        "Company.ReceiverApp");
                })
            // Enables you to execute code whenever execution of a message starts, succeeded or failed
            .RegisterEventListener<ServiceBusEventListener>()
            .PopulateAsyncApiSchemaWithEvServiceBus()
            .WithPayloadSerializer<MessagePayloadSerializer>();

        // For this sample to work, you need have an Azure service bus namespace created with the following resources:
        // - A queue named "myqueue"
        // - A topic named "mytopic" and under it :
        //     - A subscription named "mysubscription"
        //     - A subscription named "mysecondsubscription"
        builder.Services.RegisterServiceBusReception().FromQueue(ServiceBusResources.MyQueue,
            builder => { builder.RegisterReception<WeatherForecast[], WeatherMessageHandler>(); });

        builder.Services.RegisterServiceBusReception().FromSubscription(
            ServiceBusResources.MyTopic,
            ServiceBusResources.MySubscription,
            builder =>
            {
                builder.RegisterReception<WeatherForecast, WeatherEventHandler>();
                builder.RegisterReception<UserCreated, UserCreatedHandler>();
                builder.RegisterReception<UserPreferencesUpdated, UserPreferencesUpdatedHandler>();
            });
        builder.Services.RegisterServiceBusReception().FromSubscription(
            ServiceBusResources.MyTopic,
            ServiceBusResources.MySecondSubscription,
            builder => { builder.RegisterReception<WeatherForecast, SecondaryWeatherEventHandler>(); });

        builder.AddServiceDefaults();
        return builder;
    }
}