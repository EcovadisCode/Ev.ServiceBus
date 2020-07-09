using System;
using Microsoft.Azure.ServiceBus;

// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public abstract class ReceiverOptions : ClientOptions, IMessageReceiverOptions
    {
        protected ReceiverOptions(string entityPath) : base(entityPath)
        {
        }

        public Type MessageHandlerType { get; internal set; }
        public Action<MessageHandlerOptions> MessageHandlerConfig { get; internal set; }
        public Type ExceptionHandlerType { get; internal set; }
    }
}
