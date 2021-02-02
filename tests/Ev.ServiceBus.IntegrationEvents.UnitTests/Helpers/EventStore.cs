using System;
using System.Collections.Generic;

namespace Ev.ServiceBus.IntegrationEvents.UnitTests.Helpers
{
    public class EventStore
    {
        public EventStore()
        {
            Events = new List<Item>();
        }

        public List<Item> Events { get; }

        public class Item
        {
            public Type HandlerType { get; set; }
            public object Event { get; set; }
        }
    }
}
