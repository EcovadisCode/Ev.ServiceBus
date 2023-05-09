using Azure.Messaging.ServiceBus;
using Ev.ServiceBus.Dispatch;

namespace Ev.ServiceBus.Samples.Sender;

public class MyDispatchExtender : IDispatchExtender
{
    public void ExtendDispatch(ServiceBusMessage message, object dispatchPayload)
    {

    }
}
