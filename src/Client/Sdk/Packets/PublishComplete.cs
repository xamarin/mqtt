namespace System.Net.Mqtt.Sdk.Packets
{
	internal class PublishComplete : IFlowPacket, IEquatable<PublishComplete>
	{
		public PublishComplete (ushort packetId)
		{
			PacketId = packetId;
		}

		public MqttPacketType Type { get { return MqttPacketType.PublishComplete; } }

		public ushort PacketId { get; }

		public bool Equals (PublishComplete other)
		{
			if (other == null)
				return false;

			return PacketId == other.PacketId;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var publishComplete = obj as PublishComplete;

			if (publishComplete == null)
				return false;

			return Equals (publishComplete);
		}

		public static bool operator == (PublishComplete publishComplete, PublishComplete other)
		{
			if ((object)publishComplete == null || (object)other == null)
				return Object.Equals (publishComplete, other);

			return publishComplete.Equals (other);
		}

		public static bool operator != (PublishComplete publishComplete, PublishComplete other)
		{
			if ((object)publishComplete == null || (object)other == null)
				return !Object.Equals (publishComplete, other);

			return !publishComplete.Equals (other);
		}

		public override int GetHashCode ()
		{
			return PacketId.GetHashCode ();
		}
	}
}
