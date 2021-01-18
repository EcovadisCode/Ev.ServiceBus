using Ev.ServiceBus.Abstractions;
using System;
using System.Threading.Tasks;

namespace Ev.ServiceBus.Examples.AspNetCoreWeb
{
    public class WeatherMessageHandler : IMessageHandler
    {
        public Task HandleMessageAsync(MessageContext context)
        {
            var message = context.Message;

            var results = MessageParser.DeserializeMessage<WeatherForecast[]>(message);

            if (results.Length == 0)
                throw new ArgumentException("Forecast should not be empty!");

            foreach(var weather in results)
                Console.WriteLine($"{weather.Date}: {weather.Summary}");

            return Task.CompletedTask;
        }
    }
}
