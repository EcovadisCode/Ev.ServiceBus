using System;

namespace Ev.ServiceBus.IntegrationEvents
{
    public class EventTypeIdMustBeSetException : Exception
    {
        public EventTypeIdMustBeSetException() : base($"EventTypeId must be set") { }
    }
}
