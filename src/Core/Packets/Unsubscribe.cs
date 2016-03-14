using System.Collections.Generic;
using System.Linq;

namespace System.Net.Mqtt.Packets
{
	internal class Unsubscribe : IPacket, IEquatable<Unsubscribe>
	{
		public Unsubscribe (ushort packetId, params string[] topics)
		{
			PacketId = packetId;
			Topics = topics;
		}

		public PacketType Type { get { return PacketType.Unsubscribe; } }

		public ushort PacketId { get; private set; }

		public IEnumerable<string> Topics { get; private set; }

		public bool Equals (Unsubscribe other)
		{
			if (other == null)
				return false;

			return PacketId == other.PacketId &&
				Topics.SequenceEqual (other.Topics);
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var unsubscribe = obj as Unsubscribe;

			if (unsubscribe == null)
				return false;

			return Equals (unsubscribe);
		}

		public static bool operator == (Unsubscribe unsubscribe, Unsubscribe other)
		{
			if ((object)unsubscribe == null || (object)other == null)
				return Object.Equals (unsubscribe, other);

			return unsubscribe.Equals (other);
		}

		public static bool operator != (Unsubscribe unsubscribe, Unsubscribe other)
		{
			if ((object)unsubscribe == null || (object)other == null)
				return !Object.Equals (unsubscribe, other);

			return !unsubscribe.Equals (other);
		}

		public override int GetHashCode ()
		{
			return PacketId.GetHashCode () + Topics.GetHashCode ();
		}
	}
}
