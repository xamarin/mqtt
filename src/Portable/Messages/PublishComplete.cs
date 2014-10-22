using System;

namespace Hermes.Messages
{
	public class PublishComplete : IFlowMessage, IEquatable<PublishComplete>
	{
		public PublishComplete(ushort messageId)
		{
			this.MessageId = messageId;
		}

		public MessageType Type { get { return MessageType.PublishComplete; } }

		public ushort MessageId { get; private set; }

		public bool Equals(PublishComplete other)
		{
			if (other == null)
				return false;

			return this.MessageId == other.MessageId;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			var publishComplete = obj as PublishComplete;

			if (publishComplete == null)
				return false;

			return this.Equals(publishComplete);
		}

		public static bool operator ==(PublishComplete publishComplete, PublishComplete other)
		{
			if ((object)publishComplete == null || (object)other == null)
				return Object.Equals(publishComplete, other);

			return publishComplete.Equals(other);
		}

		public static bool operator !=(PublishComplete publishComplete, PublishComplete other)
		{
			if ((object)publishComplete == null || (object)other == null)
				return !Object.Equals(publishComplete, other);

			return !publishComplete.Equals(other);
		}

		public override int GetHashCode()
		{
			return this.MessageId.GetHashCode();
		}
	}
}
