using System.Threading.Tasks;
using Ev.ServiceBus.Samples.Sender;
using Ev.ServiceBus.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Ev.ServiceBus.Mvc.UnitTests
{
    public class DispatchTest
    {
        [Fact]
        public async Task EventsAreSentAtTheEndOfTheRequestExecution()
        {
            var factory = new AppFactory();
            var client = factory.CreateClient();
            var response = await client.GetAsync("weatherforecast/pushWeather");

            response.StatusCode.Should().Be(StatusCodes.Status200OK);
            var queue = factory.Services.GetQueueClientMock("myqueue", false);
            queue.Mock.Verify(o => o.SendAsync(It.Is<Message[]>(messages => messages.Length == 1)), Times.Once);

            var topic = factory.Services.GetTopicClientMock("mytopic");
            topic.Mock.Verify(o => o.SendAsync(It.Is<Message[]>(messages => messages.Length == 5)), Times.Once);
        }

        [Fact]
        public async Task FailingRequestsDoNotSendEvents()
        {
            var factory = new AppFactory();
            var client = factory.CreateClient();
            var response = await client.GetAsync("failing/pushWeather");

            response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            var queue = factory.Services.GetQueueClientMock("myqueue", false);
            queue.Mock.Verify(o => o.SendAsync(It.IsAny<Message[]>()), Times.Never);

            var topic = factory.Services.GetTopicClientMock("mytopic");
            topic.Mock.Verify(o => o.SendAsync(It.IsAny<Message[]>()), Times.Never);
        }

        private class AppFactory : WebApplicationFactory<Startup>
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
}
