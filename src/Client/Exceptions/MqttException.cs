using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
    /// <summary>
    /// Represents the base exception for any MQTT failure
    /// </summary>
	[DataContract]
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