namespace System.Net.Mqtt.Server
{
	public class TopicNotSubscribed
	{
		public string Topic { get; set; }

		public string SenderId { get; set; }

		public byte[] Payload { get; set; }
	}
}
