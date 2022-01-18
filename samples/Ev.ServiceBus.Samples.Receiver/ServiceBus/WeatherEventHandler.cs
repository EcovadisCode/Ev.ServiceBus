﻿using System.Threading;
using System.Threading.Tasks;
using Ev.ServiceBus.Abstractions.MessageReception;
using Ev.ServiceBus.Reception;
using Ev.ServiceBus.Sample.Contracts;
using Microsoft.Extensions.Logging;

namespace Ev.ServiceBus.Samples.Receiver.ServiceBus
{
    public class WeatherEventHandler : IMessageReceptionHandler<WeatherForecast>
    {
        private readonly ILogger<WeatherEventHandler> _logger;
        private readonly IMessageMetadataAccessor _metadataAccessor;

        public WeatherEventHandler(ILogger<WeatherEventHandler> logger, IMessageMetadataAccessor metadataAccessor)
        {
            _logger = logger;
            _metadataAccessor = metadataAccessor;
        }

        public Task Handle(WeatherForecast weather, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Received from 1st subscription : {weather.Date}: {weather.Summary}");
            _logger.LogWarning($"SessionId : {_metadataAccessor.Metadata.SessionId}");

            return Task.CompletedTask;
        }
    }
}
