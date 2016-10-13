using System.Linq;

namespace System.Net.Mqtt.Sdk.Packets
{
	internal class Publish : IPacket, IEquatable<Publish>
	{
		public Publish (string topic, MqttQualityOfService qualityOfService, bool retain, bool duplicated, ushort? packetId = null)
		{
			QualityOfService = qualityOfService;
			Duplicated = duplicated;
			Retain = retain;
			Topic = topic;
			PacketId = packetId;
		}

		public MqttPacketType Type { get { return MqttPacketType.Publish; } }

		public MqttQualityOfService QualityOfService { get; }

		public bool Duplicated { get; }

		public bool Retain { get; }

		public string Topic { get; }

		public ushort? PacketId { get; }

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
