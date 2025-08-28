namespace Ev.ServiceBus;

/// <summary>
/// All the user defined property names that can be in a message.
/// </summary>
public static class UserProperties
{
    public const string PayloadTypeIdProperty = "PayloadTypeId";
    public const string MessageTypeProperty = "MessageType";
    public const string IsolationKey = "IsolationKey";
    public const string IsolationApps = "IsolationApps";
}