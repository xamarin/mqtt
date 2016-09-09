using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a server operation fails
    /// </summary>
    [DataContract]
	public class MqttServerException : MqttException
	{
		public MqttServerException ()
		{
		}

		public MqttServerException (string message)
			: base (message)
		{
		}

		public MqttServerException (string message, Exception innerException)
			: base (message, innerException)
		{
		}
	}
}
