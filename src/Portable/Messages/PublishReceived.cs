using System;

namespace Hermes.Messages
{
	public class PublishReceived : IFlowMessage, IEquatable<PublishReceived>
	{
		public PublishReceived(ushort messageId)
		{
			this.MessageId = messageId;
		}

		public MessageType Type { get { return MessageType.PublishReceived; } }

		public ushort MessageId { get; private set; }

		public bool Equals(PublishReceived other)
		{
			if (other == null)
				return false;

			return this.MessageId == other.MessageId;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			var publishReceived = obj as PublishReceived;

			if (publishReceived == null)
				return false;

			return this.Equals(publishReceived);
		}

		public static bool operator ==(PublishReceived publishReceived, PublishReceived other)
		{
			if ((object)publishReceived == null || (object)other == null)
				return Object.Equals(publishReceived, other);

			return publishReceived.Equals(other);
		}

		public static bool operator !=(PublishReceived publishReceived, PublishReceived other)
		{
			if ((object)publishReceived == null || (object)other == null)
				return !Object.Equals(publishReceived, other);

			return !publishReceived.Equals(other);
		}

		public override int GetHashCode()
		{
			return this.MessageId.GetHashCode();
		}
	}
}
