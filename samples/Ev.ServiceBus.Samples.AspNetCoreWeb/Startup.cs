using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Examples.AspNetCoreWeb.ServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ev.ServiceBus.Examples.AspNetCoreWeb
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
            services.AddServiceBus(
                settings =>
                {
                    settings.WithConnection(""); // Provide a connection string here!
                });

            services.RegisterServiceBusQueue(QueuesNames.MyQueue)
                .WithCustomMessageHandler<WeatherMessageHandler>()
                .WithCustomExceptionHandler<WeatherExceptionHandler>();

            services.AddTransient<WeatherExceptionHandler>();
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
