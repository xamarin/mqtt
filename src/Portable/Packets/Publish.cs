using System;
using System.Linq;

namespace Hermes.Packets
{
	public class Publish : IPacket, IEquatable<Publish>
    {
        public Publish(string topic, QualityOfService qualityOfService, bool retain, bool duplicatedDelivery, ushort? packetId = null)
        {
            this.QualityOfService = qualityOfService;
			this.DuplicatedDelivery = duplicatedDelivery;
			this.Retain = retain;
			this.Topic = topic;
            this.PacketId = packetId;
        }

		public PacketType Type { get { return PacketType.Publish; }}

		public QualityOfService QualityOfService { get; private set; }

		public bool DuplicatedDelivery { get; private set; }

		public bool Retain { get; private set; }

        public string Topic { get; private set; }

        public ushort? PacketId { get; private set; }

		public byte[] Payload { get; set; }

		public bool Equals (Publish other)
		{
			if (other == null)
				return false;

			var equals = this.QualityOfService == other.QualityOfService &&
				this.DuplicatedDelivery == other.DuplicatedDelivery &&
				this.Retain == other.Retain &&
				this.Topic == other.Topic &&
				this.PacketId == other.PacketId;

			if(this.Payload != null) {
				equals &= this.Payload.ToList().SequenceEqual(other.Payload);
			}

			return equals;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var publish = obj as Publish;

			if (publish == null)
				return false;

			return this.Equals (publish);
		}

		public static bool operator == (Publish publish, Publish other)
		{
			if ((object)publish == null || (object)other == null)
				return Object.Equals(publish, other);

			return publish.Equals(other);
		}

		public static bool operator != (Publish publish, Publish other)
		{
			if ((object)publish == null || (object)other == null)
				return !Object.Equals(publish, other);

			return !publish.Equals(other);
		}

		public override int GetHashCode ()
		{
			var hashCode = this.QualityOfService.GetHashCode () +
				this.DuplicatedDelivery.GetHashCode () +
				this.Retain.GetHashCode () +
				this.Topic.GetHashCode ();

			if (this.Payload != null) {
				hashCode += BitConverter.ToString (this.Payload).GetHashCode ();
			}

			if (this.PacketId.HasValue) {
				hashCode += this.PacketId.Value.GetHashCode ();
			}

			return hashCode;
		}
	}
}
