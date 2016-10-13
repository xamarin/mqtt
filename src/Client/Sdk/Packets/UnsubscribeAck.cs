namespace System.Net.Mqtt.Sdk.Packets
{
	internal class UnsubscribeAck : IFlowPacket, IEquatable<UnsubscribeAck>
	{
		public UnsubscribeAck (ushort packetId)
		{
			PacketId = packetId;
		}

		public MqttPacketType Type { get { return MqttPacketType.UnsubscribeAck; } }

		public ushort PacketId { get; }

		public bool Equals (UnsubscribeAck other)
		{
			if (other == null)
				return false;

			return PacketId == other.PacketId;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var unsubscribeAck = obj as UnsubscribeAck;

			if (unsubscribeAck == null)
				return false;

			return Equals (unsubscribeAck);
		}

		public static bool operator == (UnsubscribeAck unsubscribeAck, UnsubscribeAck other)
		{
			if ((object)unsubscribeAck == null || (object)other == null)
				return Object.Equals (unsubscribeAck, other);

			return unsubscribeAck.Equals (other);
		}

		public static bool operator != (UnsubscribeAck unsubscribeAck, UnsubscribeAck other)
		{
			if ((object)unsubscribeAck == null || (object)other == null)
				return !Object.Equals (unsubscribeAck, other);

			return !unsubscribeAck.Equals (other);
		}

		public override int GetHashCode ()
		{
			return PacketId.GetHashCode ();
		}
	}
}
