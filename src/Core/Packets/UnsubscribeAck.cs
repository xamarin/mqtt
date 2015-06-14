namespace System.Net.Mqtt.Packets
{
	public class UnsubscribeAck : IFlowPacket, IEquatable<UnsubscribeAck>
    {
        public UnsubscribeAck(ushort packetId)
        {
            this.PacketId = packetId;
        }

		public PacketType Type { get { return PacketType.UnsubscribeAck; }}

        public ushort PacketId { get; private set; }

		public bool Equals (UnsubscribeAck other)
		{
			if (other == null)
				return false;

			return this.PacketId == other.PacketId;
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
			return this.PacketId.GetHashCode ();
		}
    }
}
