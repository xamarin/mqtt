using System;

namespace Hermes.Messages
{
	public class Subscription : IEquatable<Subscription>
    {
        public Subscription(string topic, QualityOfService requestedQos)
        {
            this.Topic = topic;
            this.RequestedQualityOfService = requestedQos;
        }

        public string Topic { get; set; }

        public QualityOfService RequestedQualityOfService { get; set; }

		public bool Equals (Subscription other)
		{
			if (other == null)
				return false;

			return this.Topic == other.Topic &&
				this.RequestedQualityOfService == other.RequestedQualityOfService;
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
			return this.Topic.GetHashCode () + this.RequestedQualityOfService.GetHashCode ();
		}
	}
}
