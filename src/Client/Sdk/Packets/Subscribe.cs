using System.Collections.Generic;
using System.Linq;

namespace System.Net.Mqtt.Sdk.Packets
{
	internal class Subscribe : IFlowPacket, IEquatable<Subscribe>
	{
		public Subscribe (ushort packetId, params Subscription[] subscriptions)
		{
			PacketId = packetId;
			Subscriptions = subscriptions;
		}

		public MqttPacketType Type { get { return MqttPacketType.Subscribe; } }

		public ushort PacketId { get; }

		public IEnumerable<Subscription> Subscriptions { get; }

		public bool Equals (Subscribe other)
		{
			if (other == null)
				return false;

			return PacketId == other.PacketId &&
				Subscriptions.SequenceEqual (other.Subscriptions);
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var subscribe = obj as Subscribe;

			if (subscribe == null)
				return false;

			return Equals (subscribe);
		}

		public static bool operator == (Subscribe subscribe, Subscribe other)
		{
			if ((object)subscribe == null || (object)other == null)
				return Object.Equals (subscribe, other);

			return subscribe.Equals (other);
		}

		public static bool operator != (Subscribe subscribe, Subscribe other)
		{
			if ((object)subscribe == null || (object)other == null)
				return !Object.Equals (subscribe, other);

			return !subscribe.Equals (other);
		}

		public override int GetHashCode ()
		{
			return PacketId.GetHashCode () + Subscriptions.GetHashCode ();
		}
	}
}
