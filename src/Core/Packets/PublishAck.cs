namespace System.Net.Mqtt.Packets
{
	internal class PublishAck : IFlowPacket, IEquatable<PublishAck>
    {
        public PublishAck(ushort packetId)
        {
            this.PacketId = packetId;
        }

		public PacketType Type { get { return PacketType.PublishAck; }}

        public ushort PacketId { get; private set; }

		public bool Equals (PublishAck other)
		{
			if (other == null)
				return false;

			return this.PacketId == other.PacketId;
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
			return this.PacketId.GetHashCode ();
		}
	}
}
