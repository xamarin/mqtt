namespace System.Net.Mqtt.Sdk.Packets
{
	internal class Subscription : IEquatable<Subscription>
	{
		public Subscription (string topicFilter, MqttQualityOfService requestedQos)
		{
			TopicFilter = topicFilter;
			MaximumQualityOfService = requestedQos;
		}

		public string TopicFilter { get; set; }

		public MqttQualityOfService MaximumQualityOfService { get; set; }

		public bool Equals (Subscription other)
		{
			if (other == null)
				return false;

			return TopicFilter == other.TopicFilter &&
				MaximumQualityOfService == other.MaximumQualityOfService;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var subscription = obj as Subscription;

			if (subscription == null)
				return false;

			return Equals (subscription);
		}

		public static bool operator == (Subscription subscription, Subscription other)
		{
			if ((object)subscription == null || (object)other == null)
				return Object.Equals (subscription, other);

			return subscription.Equals (other);
		}

		public static bool operator != (Subscription subscription, Subscription other)
		{
			if ((object)subscription == null || (object)other == null)
				return !Object.Equals (subscription, other);

			return !subscription.Equals (other);
		}

		public override int GetHashCode ()
		{
			return TopicFilter.GetHashCode () + MaximumQualityOfService.GetHashCode ();
		}
	}
}
