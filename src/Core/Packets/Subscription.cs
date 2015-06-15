namespace System.Net.Mqtt.Packets
{
	internal class Subscription : IEquatable<Subscription>
    {
        public Subscription(string topicFilter, QualityOfService requestedQos)
        {
            this.TopicFilter = topicFilter;
            this.MaximumQualityOfService = requestedQos;
        }

        public string TopicFilter { get; set; }

        public QualityOfService MaximumQualityOfService { get; set; }

		public bool Equals (Subscription other)
		{
			if (other == null)
				return false;

			return this.TopicFilter == other.TopicFilter &&
				this.MaximumQualityOfService == other.MaximumQualityOfService;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var subscription = obj as Subscription;

			if (subscription == null)
				return false;

			return this.Equals (subscription);
		}

		public static bool operator == (Subscription subscription, Subscription other)
		{
			if ((object)subscription == null || (object)other == null)
				return Object.Equals(subscription, other);

			return subscription.Equals(other);
		}

		public static bool operator != (Subscription subscription, Subscription other)
		{
			if ((object)subscription == null || (object)other == null)
				return !Object.Equals(subscription, other);

			return !subscription.Equals(other);
		}

		public override int GetHashCode ()
		{
			return this.TopicFilter.GetHashCode () + this.MaximumQualityOfService.GetHashCode ();
		}
	}
}
