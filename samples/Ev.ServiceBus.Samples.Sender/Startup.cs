using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.AsyncApi;
using Ev.ServiceBus.Mvc;
using Ev.ServiceBus.Sample.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Saunter;
using Saunter.AsyncApiSchema.v2;

namespace Ev.ServiceBus.Samples.Sender
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .AddServiceBusMvcIntegration();

            services.AddAsyncApiSchemaGeneration(
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

            services.AddControllers();
            services.AddServiceBus<MessagePayloadSerializer>(
                settings =>
                {
                    // Provide a connection string here !
                    settings.WithConnection("Endpoint=sb://yourconnection.servicebus.windows.net/;SharedAccessKeyName=yourkeyh;SharedAccessKey=ae6pTuOBAFDH2y7xJJf9BFubZGxXMToN6B9NiVgLnbQ=", new ServiceBusClientOptions());
                })
                .PopulateAsyncApiSchemaWithEvServiceBus();

            // For this sample to work, you need have an Azure service bus namespace created with the following resources:
            // - A queue named "myqueue"
            // - A topic named "mytopic" and under it :
            //     - A subscription named "mysubscription"
            //     - A subscription named "mysecondsubscription"
            services.RegisterServiceBusDispatch().ToQueue(ServiceBusResources.MyQueue, builder =>
            {
                builder.RegisterDispatch<WeatherForecast[]>();
            });

            services.RegisterServiceBusDispatch().ToTopic(ServiceBusResources.MyTopic, builder =>
            {
                builder.RegisterDispatch<WeatherForecast>();
            });

            services.AddHealthChecks()
                .AddEvServiceBusChecks();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapAsyncApiDocuments();
                endpoints.MapAsyncApiUi();
            });
        }
    }
}
