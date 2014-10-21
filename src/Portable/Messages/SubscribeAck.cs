using System;
using System.Collections.Generic;
using System.Linq;

namespace Hermes.Messages
{
	public class SubscribeAck : Message, IEquatable<SubscribeAck>
    {
        public SubscribeAck(ushort messageId, params SubscribeReturnCode[] returnCodes)
            : base(MessageType.SubscribeAck)
        {
			this.MessageId = messageId;
			this.ReturnCodes = returnCodes;
        }

        public ushort MessageId { get; private set; }

        public IEnumerable<SubscribeReturnCode> ReturnCodes { get; private set; }

		public bool Equals (SubscribeAck other)
		{
			if (other == null)
				return false;

			return this.MessageId == other.MessageId &&
				this.ReturnCodes.SequenceEqual(other.ReturnCodes);
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var subscribeAck = obj as SubscribeAck;

			if (subscribeAck == null)
				return false;

			return this.Equals (subscribeAck);
		}

		public static bool operator == (SubscribeAck subscribeAck, SubscribeAck other)
		{
			if ((object)subscribeAck == null || (object)other == null)
				return Object.Equals(subscribeAck, other);

			return subscribeAck.Equals(other);
		}

		public static bool operator != (SubscribeAck subscribeAck, SubscribeAck other)
		{
			if ((object)subscribeAck == null || (object)other == null)
				return !Object.Equals(subscribeAck, other);

			return !subscribeAck.Equals(other);
		}

		public override int GetHashCode ()
		{
			return this.MessageId.GetHashCode () + this.ReturnCodes.GetHashCode ();
		}
	}
}
