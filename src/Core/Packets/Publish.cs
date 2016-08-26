using System.Linq;

namespace System.Net.Mqtt.Packets
{
	internal class Publish : IPacket, IEquatable<Publish>
	{
		public Publish (string topic, QualityOfService qualityOfService, bool retain, bool duplicated, ushort? packetId = null)
		{
			QualityOfService = qualityOfService;
			Duplicated = duplicated;
			Retain = retain;
			Topic = topic;
			PacketId = packetId;
		}

		public PacketType Type { get { return PacketType.Publish; } }

		public QualityOfService QualityOfService { get; private set; }

		public bool Duplicated { get; private set; }

		public bool Retain { get; private set; }

		public string Topic { get; private set; }

		public ushort? PacketId { get; private set; }

		public byte[] Payload { get; set; }

		public bool Equals (Publish other)
		{
			if (other == null)
				return false;

			var equals = QualityOfService == other.QualityOfService &&
				Duplicated == other.Duplicated &&
				Retain == other.Retain &&
				Topic == other.Topic &&
				PacketId == other.PacketId;

			if (Payload != null) {
				equals &= Payload.ToList ().SequenceEqual (other.Payload);
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

			return Equals (publish);
		}

		public static bool operator == (Publish publish, Publish other)
		{
			if ((object)publish == null || (object)other == null)
				return Object.Equals (publish, other);

			return publish.Equals (other);
		}

		public static bool operator != (Publish publish, Publish other)
		{
			if ((object)publish == null || (object)other == null)
				return !Object.Equals (publish, other);

			return !publish.Equals (other);
		}

		public override int GetHashCode ()
		{
			var hashCode = QualityOfService.GetHashCode () +
				Duplicated.GetHashCode () +
				Retain.GetHashCode () +
				Topic.GetHashCode ();

			if (Payload != null) {
				hashCode += BitConverter.ToString (Payload).GetHashCode ();
			}

			if (PacketId.HasValue) {
				hashCode += PacketId.Value.GetHashCode ();
			}

			return hashCode;
		}
	}
}
