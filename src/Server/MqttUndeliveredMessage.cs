namespace System.Net.Mqtt.Server
{
	public class MqttUndeliveredMessage
    {
		public string SenderId { get; set; }

		public MqttApplicationMessage Message { get; set; }
	}
}
