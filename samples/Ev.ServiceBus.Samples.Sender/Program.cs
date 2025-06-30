using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.AsyncApi;
using Ev.ServiceBus.Mvc;
using Ev.ServiceBus.Prometheus;
using Ev.ServiceBus.Sample.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using Saunter;
using Saunter.AsyncApiSchema.v2;

namespace Ev.ServiceBus.Samples.Sender;

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

        app.UseMetricServer();
        app.UseHttpMetrics();
        app.UseHttpsRedirection();

        app.UseSwagger();
        app.UseSwaggerUI();
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
        builder.Services.AddHostedService<ServicebusPrometheusExporter>();
        builder.Services.AddMvc()
            .AddServiceBusMvcIntegration();

        builder.Services.AddAsyncApiSchemaGeneration(
            options =>
            {
                options.AsyncApi = new AsyncApiDocument()
                {
                    Info = new Info("Receiver API", "1.0.0")
                    {
                        Description = "Sample sender project",
                    }
                };
            });

        builder.Services.AddControllers();
        builder.Services.AddServiceBus(
                settings =>
                {
                    // Provide a connection string here !
                    settings.WithConnection(
                        "Endpoint=sb://yourconnection.servicebus.windows.net/;SharedAccessKeyName=yourkeyh;SharedAccessKey=ae6pTuOBAFDH2y7xJJf9BFubZGxXMToN6B9NiVgLnbQ=",
                        new ServiceBusClientOptions());
                    // In case if you want to use isolation mode, uncomment these 2 lines:
                    // settings.UseIsolation = true;
                    // settings.IsolationKey = "your-isolation-key";
                })
            .PopulateAsyncApiSchemaWithEvServiceBus()
            .RegisterDispatchExtender<MyDispatchExtender>()
            .WithPayloadSerializer<MessagePayloadSerializer>();

        // For this sample to work, you need have an Azure service bus namespace created with the following resources:
        // - A queue named "myqueue"
        // - A topic named "mytopic" and under it :
        //     - A subscription named "mysubscription"
        //     - A subscription named "mysecondsubscription"
        builder.Services.RegisterServiceBusDispatch().ToQueue(ServiceBusResources.MyQueue,
            builder => { builder.RegisterDispatch<WeatherForecast[]>(); });

        builder.Services.RegisterServiceBusDispatch().ToTopic(ServiceBusResources.MyTopic, builder =>
        {
            builder.RegisterDispatch<WeatherForecast>();
            builder.RegisterDispatch<UserCreated>().CustomizePayloadTypeId("User/UserCreated");
        });

        builder.Services.AddHealthChecks()
            .AddEvServiceBusChecks();
        builder.AddServiceDefaults();
        return builder;
    }
}