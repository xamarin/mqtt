using System.Collections.Generic;
using System.Linq;

namespace System.Net.Mqtt.Packets
{
	internal class SubscribeAck : IPacket, IEquatable<SubscribeAck>
	{
		public SubscribeAck (ushort packetId, params SubscribeReturnCode[] returnCodes)
		{
			PacketId = packetId;
			ReturnCodes = returnCodes;
		}

		public PacketType Type { get { return PacketType.SubscribeAck; } }

		public ushort PacketId { get; private set; }

		public IEnumerable<SubscribeReturnCode> ReturnCodes { get; private set; }

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
