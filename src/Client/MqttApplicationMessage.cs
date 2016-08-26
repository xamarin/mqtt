namespace System.Net.Mqtt
{
	public partial class MqttApplicationMessage
	{
		public MqttApplicationMessage ()
		{

		}

		public MqttApplicationMessage (string topic, byte[] payload)
		{
			Topic = topic;
			Payload = payload;
		}

		public string Topic { get; set; }

		public byte[] Payload { get; set; }
	}
}
