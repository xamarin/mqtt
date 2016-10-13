namespace System.Net.Mqtt.Sdk.Storage
{
	internal enum PendingMessageStatus
	{
		PendingToAcknowledge = 1,
		PendingToSend = 2
	}

	internal class PendingMessage
	{
		public PendingMessageStatus Status { get; set; }

		public MqttQualityOfService QualityOfService { get; set; }

		public bool Duplicated { get; set; }

		public bool Retain { get; set; }

		public string Topic { get; set; }

		public ushort? PacketId { get; set; }

		public byte[] Payload { get; set; }
	}
}
