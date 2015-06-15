﻿namespace System.Net.Mqtt.Packets
{
	internal class PublishComplete : IFlowPacket, IEquatable<PublishComplete>
	{
		public PublishComplete(ushort packetId)
		{
			this.PacketId = packetId;
		}

		public PacketType Type { get { return PacketType.PublishComplete; } }

		public ushort PacketId { get; private set; }

		public bool Equals(PublishComplete other)
		{
			if (other == null)
				return false;

			return this.PacketId == other.PacketId;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			var publishComplete = obj as PublishComplete;

			if (publishComplete == null)
				return false;

			return this.Equals(publishComplete);
		}

		public static bool operator ==(PublishComplete publishComplete, PublishComplete other)
		{
			if ((object)publishComplete == null || (object)other == null)
				return Object.Equals(publishComplete, other);

			return publishComplete.Equals(other);
		}

		public static bool operator !=(PublishComplete publishComplete, PublishComplete other)
		{
			if ((object)publishComplete == null || (object)other == null)
				return !Object.Equals(publishComplete, other);

			return !publishComplete.Equals(other);
		}

		public override int GetHashCode()
		{
			return this.PacketId.GetHashCode();
		}
	}
}
