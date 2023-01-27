using System;
using System.Linq;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions;
using Ev.ServiceBus.Abstractions.Batching;
using Ev.ServiceBus.Sample.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Ev.ServiceBus.Samples.Sender.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class BatchController : ControllerBase
    {
        private readonly IDispatchSender _sender;
        private readonly IMessageBatcher _batcher;

        public BatchController(IDispatchSender sender, IMessageBatcher batcher)
        {
            _sender = sender;
            _batcher = batcher;
        }

        [HttpPost]
        public async Task Send([FromBody]Payload payload)
        {
            var rng = new Random();
            var forecasts = Enumerable
                .Range(0, payload.Count)
                .Select(_ => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(1),
                    TemperatureC = rng.Next(-20, 55),
                    Summary = payload.Content
                })
                .ToArray();
            var batches = await _batcher.CalculateBatches(forecasts);
            foreach (var batch in batches)
            {
                await _sender.SendDispatches(batch);
            }
        }

        public sealed class Payload
        {
            public int Count { get; set; }
            public string Content { get; set; }
        }
    }
}
