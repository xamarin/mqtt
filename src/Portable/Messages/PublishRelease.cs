using System;

namespace Hermes.Messages
{
	public class PublishRelease : IFlowMessage, IEquatable<PublishRelease>
    {
        public PublishRelease(ushort messageId)
        {
            this.MessageId = messageId;
        }

		public MessageType Type { get { return MessageType.PublishRelease; }}

        public ushort MessageId { get; private set; }

		public bool Equals (PublishRelease other)
		{
			if (other == null)
				return false;

			return this.MessageId == other.MessageId;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var publishRelease = obj as PublishRelease;

			if (publishRelease == null)
				return false;

			return this.Equals (publishRelease);
		}

		public static bool operator == (PublishRelease publishRelease, PublishRelease other)
		{
			if ((object)publishRelease == null || (object)other == null)
				return Object.Equals(publishRelease, other);

			return publishRelease.Equals(other);
		}

		public static bool operator != (PublishRelease publishRelease, PublishRelease other)
		{
			if ((object)publishRelease == null || (object)other == null)
				return !Object.Equals(publishRelease, other);

			return !publishRelease.Equals(other);
		}

		public override int GetHashCode ()
		{
			return this.MessageId.GetHashCode ();
		}
    }
}
