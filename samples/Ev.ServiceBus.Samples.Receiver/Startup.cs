using Ev.ServiceBus.Sample.Contracts;
using Ev.ServiceBus.Samples.Receiver.ServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ev.ServiceBus.Samples.Receiver
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddServiceBus<MessagePayloadSerializer>(
                settings =>
                {
                    settings.WithConnection(""); // Provide a connection string here!
                })
                // Enables you to execute code whenever execution of a message starts, succeeded or failed
                .RegisterEventListener<ServiceBusEventListener>();

            // For this sample to work, you need have an Azure service bus namespace created with the following resources:
            // - A queue named "myqueue"
            // - A topic named "mytopic" and under it :
            //     - A subscription named "mysubscription"
            //     - A subscription named "mysecondsubscription"
            services.RegisterServiceBusReception().FromQueue(ServiceBusResources.MyQueue, builder =>
            {
                builder.RegisterReception<WeatherForecast[], WeatherMessageHandler>();
            });

            services.RegisterServiceBusReception().FromSubscription(
                ServiceBusResources.MyTopic,
                ServiceBusResources.MySubscription,
                builder =>
                {
                    builder.RegisterReception<WeatherForecast, WeatherEventHandler>();
                });
            services.RegisterServiceBusReception().FromSubscription(
                ServiceBusResources.MyTopic,
                ServiceBusResources.MySecondSubscription,
                builder =>
                {
                    builder.RegisterReception<WeatherForecast, SecondaryWeatherEventHandler>();
                });
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
            });
        }
    }
}
