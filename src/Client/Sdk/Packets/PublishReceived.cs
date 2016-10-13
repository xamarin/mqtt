namespace System.Net.Mqtt.Sdk.Packets
{
	internal class PublishReceived : IFlowPacket, IEquatable<PublishReceived>
	{
		public PublishReceived (ushort packetId)
		{
			PacketId = packetId;
		}

		public MqttPacketType Type { get { return MqttPacketType.PublishReceived; } }

		public ushort PacketId { get; }

		public bool Equals (PublishReceived other)
		{
			if (other == null)
				return false;

			return PacketId == other.PacketId;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var publishReceived = obj as PublishReceived;

			if (publishReceived == null)
				return false;

			return Equals (publishReceived);
		}

		public static bool operator == (PublishReceived publishReceived, PublishReceived other)
		{
			if ((object)publishReceived == null || (object)other == null)
				return Object.Equals (publishReceived, other);

			return publishReceived.Equals (other);
		}

		public static bool operator != (PublishReceived publishReceived, PublishReceived other)
		{
			if ((object)publishReceived == null || (object)other == null)
				return !Object.Equals (publishReceived, other);

			return !publishReceived.Equals (other);
		}

		public override int GetHashCode ()
		{
			return PacketId.GetHashCode ();
		}
	}
}
