namespace System.Net.Mqtt.Exceptions
{
	public class MqttException : Exception
	{
		public MqttException ()
		{
		}

		public MqttException (string message) : base (message)
		{
		}

		public MqttException (string message, Exception innerException) : base (message, innerException)
		{
		}
	}
}