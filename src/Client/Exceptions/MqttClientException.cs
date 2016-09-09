using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a client operation fails
    /// </summary>
    [DataContract]
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
