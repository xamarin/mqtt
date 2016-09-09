using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
    /// <summary>
    /// The exception thrown when an operation with the data repository used to store MQTT information fails
    /// </summary>
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
