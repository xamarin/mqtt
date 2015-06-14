using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Storage
{
	public enum PendingMessageStatus
	{
		PendingToAcknowledge = 1,
		PendingToSend = 2
	}

	public class PendingMessage
	{
		public PendingMessageStatus Status { get; set; }

		public QualityOfService QualityOfService { get; set; }

		public bool Duplicated { get; set; }

		public bool Retain { get; set; }

		public string Topic { get; set; }

		public ushort? PacketId { get; set; }

		public byte[] Payload { get; set; }
	}
}
