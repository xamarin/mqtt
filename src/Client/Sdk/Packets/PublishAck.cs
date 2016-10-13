namespace System.Net.Mqtt.Sdk.Packets
{
	internal class PublishAck : IFlowPacket, IEquatable<PublishAck>
	{
		public PublishAck (ushort packetId)
		{
			PacketId = packetId;
		}

		public MqttPacketType Type { get { return MqttPacketType.PublishAck; } }

		public ushort PacketId { get; }

		public bool Equals (PublishAck other)
		{
			if (other == null)
				return false;

			return PacketId == other.PacketId;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var publishAck = obj as PublishAck;

			if (publishAck == null)
				return false;

			return Equals (publishAck);
		}

		public static bool operator == (PublishAck publishAck, PublishAck other)
		{
			if ((object)publishAck == null || (object)other == null)
				return Object.Equals (publishAck, other);

			return publishAck.Equals (other);
		}

		public static bool operator != (PublishAck publishAck, PublishAck other)
		{
			if ((object)publishAck == null || (object)other == null)
				return !Object.Equals (publishAck, other);

			return !publishAck.Equals (other);
		}

		public override int GetHashCode ()
		{
			return PacketId.GetHashCode ();
		}
	}
}
