﻿using Ev.ServiceBus.Samples.Receiver;
using Ev.ServiceBus.UnitTests.Helpers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.AsyncApi.UnitTests
{
    public class ReceiverAppFactory : WebApplicationFactory<Startup>
    {
        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return WebHost.CreateDefaultBuilder()
                .UseUrls("http://localhost")
                .UseStartup<Startup>();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            SetupConsoleLogging(builder);

            builder.ConfigureTestServices(
                services =>
                {
                    services.OverrideClientFactories();
                });
        }

        private static void SetupConsoleLogging(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole(options =>
                {
#pragma warning disable 618
                    options.DisableColors = false;
                    options.IncludeScopes = true;
#pragma warning restore 618
                });
            });
        }
    }
}
