using System;

namespace Hermes.Messages
{
	public class PublishAck : IFlowMessage, IEquatable<PublishAck>
    {
        public PublishAck(ushort messageId)
        {
            this.MessageId = messageId;
        }

		public MessageType Type { get { return MessageType.PublishAck; }}

        public ushort MessageId { get; private set; }

		public bool Equals (PublishAck other)
		{
			if (other == null)
				return false;

			return this.MessageId == other.MessageId;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var publishAck = obj as PublishAck;

			if (publishAck == null)
				return false;

			return this.Equals (publishAck);
		}

		public static bool operator == (PublishAck publishAck, PublishAck other)
		{
			if ((object)publishAck == null || (object)other == null)
				return Object.Equals(publishAck, other);

			return publishAck.Equals(other);
		}

		public static bool operator != (PublishAck publishAck, PublishAck other)
		{
			if ((object)publishAck == null || (object)other == null)
				return !Object.Equals(publishAck, other);

			return !publishAck.Equals(other);
		}

		public override int GetHashCode ()
		{
			return this.MessageId.GetHashCode ();
		}
	}
}
