namespace Hermes.Messages
{
	public class Subscription
    {
        public Subscription(string topic, QualityOfService requestedQos)
        {
            this.Topic = topic;
            this.RequestedQualityOfService = requestedQos;
        }

        public string Topic { get; private set; }

        public QualityOfService RequestedQualityOfService { get; private set; }
    }
}
