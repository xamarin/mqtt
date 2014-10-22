using System;

namespace Hermes.Messages
{
	public class UnsubscribeAck : IFlowMessage, IEquatable<UnsubscribeAck>
    {
        public UnsubscribeAck(ushort messageId)
        {
            this.MessageId = messageId;
        }

		public MessageType Type { get { return MessageType.UnsubscribeAck; }}

        public ushort MessageId { get; private set; }

		public bool Equals (UnsubscribeAck other)
		{
			if (other == null)
				return false;

			return this.MessageId == other.MessageId;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var unsubscribeAck = obj as UnsubscribeAck;

			if (unsubscribeAck == null)
				return false;

			return this.Equals (unsubscribeAck);
		}

		public static bool operator == (UnsubscribeAck unsubscribeAck, UnsubscribeAck other)
		{
			if ((object)unsubscribeAck == null || (object)other == null)
				return Object.Equals(unsubscribeAck, other);

			return unsubscribeAck.Equals(other);
		}

		public static bool operator != (UnsubscribeAck unsubscribeAck, UnsubscribeAck other)
		{
			if ((object)unsubscribeAck == null || (object)other == null)
				return !Object.Equals(unsubscribeAck, other);

			return !unsubscribeAck.Equals(other);
		}

		public override int GetHashCode ()
		{
			return this.MessageId.GetHashCode ();
		}
    }
}
