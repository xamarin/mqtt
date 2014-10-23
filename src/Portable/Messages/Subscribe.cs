using System;
using System.Collections.Generic;
using System.Linq;

namespace Hermes.Messages
{
	public class Subscribe : IFlowMessage, IEquatable<Subscribe>
    {
        public Subscribe(ushort messageId, params Subscription[] subscriptions)
        {
			this.MessageId = messageId;
            this.Subscriptions = subscriptions;
        }

		public MessageType Type { get { return MessageType.Subscribe; }}

        public ushort MessageId { get; private set; }

        public IEnumerable<Subscription> Subscriptions { get; private set; }

		public bool Equals (Subscribe other)
		{
			if (other == null)
				return false;

			return this.MessageId == other.MessageId &&
				this.Subscriptions.SequenceEqual(other.Subscriptions);
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var subscribe = obj as Subscribe;

			if (subscribe == null)
				return false;

			return this.Equals (subscribe);
		}

		public static bool operator == (Subscribe subscribe, Subscribe other)
		{
			if ((object)subscribe == null || (object)other == null)
				return Object.Equals(subscribe, other);

			return subscribe.Equals(other);
		}

		public static bool operator != (Subscribe subscribe, Subscribe other)
		{
			if ((object)subscribe == null || (object)other == null)
				return !Object.Equals(subscribe, other);

			return !subscribe.Equals(other);
		}

		public override int GetHashCode ()
		{
			return this.MessageId.GetHashCode () + this.Subscriptions.GetHashCode ();
		}
	}
}
