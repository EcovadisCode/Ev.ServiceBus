using Azure.Messaging.ServiceBus;

namespace Ev.ServiceBus.Dispatch;

public interface IDispatchExtender
{
    void ExtendDispatch(ServiceBusMessage message, object dispatchPayload);
}
