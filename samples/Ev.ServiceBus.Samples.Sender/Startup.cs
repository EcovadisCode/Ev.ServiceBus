using Ev.ServiceBus.Sample.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            services.AddControllers();
            services.AddServiceBus<MessagePayloadSerializer>(
                settings =>
                {
                    settings.WithConnection(""); // Provide a connection string here!
                });

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
