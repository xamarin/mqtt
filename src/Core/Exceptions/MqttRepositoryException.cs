using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
	[DataContract]
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
