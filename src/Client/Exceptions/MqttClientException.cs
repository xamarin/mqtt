namespace System.Net.Mqtt.Exceptions
{
	public class MqttClientException : MqttException
    {
		public MqttClientException ()
		{
		}

		public MqttClientException (string message)
			: base (message)
		{
		}

		public MqttClientException (string message, Exception innerException)
			: base (message, innerException)
		{
		}
	}
}
