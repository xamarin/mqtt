namespace System.Net.Mqtt.Exceptions
{
	public class MqttRepositoryException : MqttException
    {
		public MqttRepositoryException ()
		{
		}

		public MqttRepositoryException (string message) : base (message)
		{
		}

		public MqttRepositoryException (string message, Exception innerException) : base (message, innerException)
		{
		}
	}
}
