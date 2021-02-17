// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public class TopicOptions : ClientOptions
    {
        public TopicOptions(string topicName, bool strictMode) : base(topicName, ClientType.Topic, strictMode)
        {
            TopicName = topicName;
        }

        public string TopicName { get; }
    }
}
