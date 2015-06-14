using System.Collections.Generic;
using System.Linq;

namespace System.Net.Mqtt.Packets
{
	public class Subscribe : IFlowPacket, IEquatable<Subscribe>
    {
        public Subscribe(ushort packetId, params Subscription[] subscriptions)
        {
			this.PacketId = packetId;
            this.Subscriptions = subscriptions;
        }

		public PacketType Type { get { return PacketType.Subscribe; }}

        public ushort PacketId { get; private set; }

        public IEnumerable<Subscription> Subscriptions { get; private set; }

		public bool Equals (Subscribe other)
		{
			if (other == null)
				return false;

			return this.PacketId == other.PacketId &&
				this.Subscriptions.SequenceEqual(other.Subscriptions);
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var subscribe = obj as Subscribe;

			if (subscribe == null)
				return false;

			return this.Equals (subscribe);
		}

		public static bool operator == (Subscribe subscribe, Subscribe other)
		{
			if ((object)subscribe == null || (object)other == null)
				return Object.Equals(subscribe, other);

			return subscribe.Equals(other);
		}

		public static bool operator != (Subscribe subscribe, Subscribe other)
		{
			if ((object)subscribe == null || (object)other == null)
				return !Object.Equals(subscribe, other);

			return !subscribe.Equals(other);
		}

		public override int GetHashCode ()
		{
			return this.PacketId.GetHashCode () + this.Subscriptions.GetHashCode ();
		}
	}
}
