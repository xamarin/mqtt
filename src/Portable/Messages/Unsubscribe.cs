using System;
using System.Collections.Generic;
using System.Linq;

namespace Hermes.Messages
{
	public class Unsubscribe : IMessage, IEquatable<Unsubscribe>
	{
		public Unsubscribe(ushort messageId, params string[] topics)
		{
			this.MessageId = messageId;
			this.Topics = topics;
		}

		public MessageType Type { get { return MessageType.Unsubscribe; } }

		public ushort MessageId { get; private set; }

		public IEnumerable<string> Topics { get; private set; }

		public bool Equals(Unsubscribe other)
		{
			if (other == null)
				return false;

			return this.MessageId == other.MessageId &&
				this.Topics.SequenceEqual(other.Topics);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			var unsubscribe = obj as Unsubscribe;

			if (unsubscribe == null)
				return false;

			return this.Equals(unsubscribe);
		}

		public static bool operator ==(Unsubscribe unsubscribe, Unsubscribe other)
		{
			if ((object)unsubscribe == null || (object)other == null)
				return Object.Equals(unsubscribe, other);

			return unsubscribe.Equals(other);
		}

		public static bool operator !=(Unsubscribe unsubscribe, Unsubscribe other)
		{
			if ((object)unsubscribe == null || (object)other == null)
				return !Object.Equals(unsubscribe, other);

			return !unsubscribe.Equals(other);
		}

		public override int GetHashCode()
		{
			return this.MessageId.GetHashCode() + this.Topics.GetHashCode();
		}
	}
}
