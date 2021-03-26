// ReSharper disable once CheckNamespace
namespace Ev.ServiceBus.Abstractions
{
    public class TopicOptions : ClientOptions
    {
        public TopicOptions(string topicName, bool strictMode) : base(topicName, ClientType.Topic, strictMode)
        {
            TopicName = topicName;
        }

        /// <summary>
        /// The name of the topic
        /// </summary>
        public string TopicName { get; }
    }
}
