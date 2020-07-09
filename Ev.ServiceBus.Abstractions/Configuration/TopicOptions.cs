// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public class TopicOptions : ClientOptions
    {
        public TopicOptions(string topicName) : base(topicName)
        {
            TopicName = topicName;
        }

        public string TopicName { get; }
    }
}
