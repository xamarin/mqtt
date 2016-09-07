namespace System.Net.Mqtt
{
	public class MqttUndeliveredMessage
    {
		public string SenderId { get; set; }

		public MqttApplicationMessage Message { get; set; }
	}
}
