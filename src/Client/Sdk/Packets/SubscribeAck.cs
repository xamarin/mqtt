using System.Collections.Generic;
using System.Linq;

namespace System.Net.Mqtt.Sdk.Packets
{
	internal class SubscribeAck : IPacket, IEquatable<SubscribeAck>
	{
		public SubscribeAck (ushort packetId, params SubscribeReturnCode[] returnCodes)
		{
			PacketId = packetId;
			ReturnCodes = returnCodes;
		}

		public MqttPacketType Type { get { return MqttPacketType.SubscribeAck; } }

		public ushort PacketId { get; }

		public IEnumerable<SubscribeReturnCode> ReturnCodes { get; }

		public bool Equals (SubscribeAck other)
		{
			if (other == null)
				return false;

			return PacketId == other.PacketId &&
				ReturnCodes.SequenceEqual (other.ReturnCodes);
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var subscribeAck = obj as SubscribeAck;

			if (subscribeAck == null)
				return false;

			return Equals (subscribeAck);
		}

		public static bool operator == (SubscribeAck subscribeAck, SubscribeAck other)
		{
			if ((object)subscribeAck == null || (object)other == null)
				return Object.Equals (subscribeAck, other);

			return subscribeAck.Equals (other);
		}

		public static bool operator != (SubscribeAck subscribeAck, SubscribeAck other)
		{
			if ((object)subscribeAck == null || (object)other == null)
				return !Object.Equals (subscribeAck, other);

			return !subscribeAck.Equals (other);
		}

		public override int GetHashCode ()
		{
			return PacketId.GetHashCode () + ReturnCodes.GetHashCode ();
		}
	}
}
