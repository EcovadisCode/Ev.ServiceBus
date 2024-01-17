// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions;

public class TopicOptions : ClientOptions
{
    public TopicOptions(string topicName) : base(topicName, ClientType.Topic)
    {
        TopicName = topicName;
    }

    /// <summary>
    /// The name of the topic
    /// </summary>
    public string TopicName { get; }
}