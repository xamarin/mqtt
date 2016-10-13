namespace System.Net.Mqtt.Sdk.Packets
{
	internal class PublishRelease : IFlowPacket, IEquatable<PublishRelease>
	{
		public PublishRelease (ushort packetId)
		{
			PacketId = packetId;
		}

		public MqttPacketType Type { get { return MqttPacketType.PublishRelease; } }

		public ushort PacketId { get; }

		public bool Equals (PublishRelease other)
		{
			if (other == null)
				return false;

			return PacketId == other.PacketId;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var publishRelease = obj as PublishRelease;

			if (publishRelease == null)
				return false;

			return Equals (publishRelease);
		}

		public static bool operator == (PublishRelease publishRelease, PublishRelease other)
		{
			if ((object)publishRelease == null || (object)other == null)
				return Object.Equals (publishRelease, other);

			return publishRelease.Equals (other);
		}

		public static bool operator != (PublishRelease publishRelease, PublishRelease other)
		{
			if ((object)publishRelease == null || (object)other == null)
				return !Object.Equals (publishRelease, other);

			return !publishRelease.Equals (other);
		}

		public override int GetHashCode ()
		{
			return PacketId.GetHashCode ();
		}
	}
}
