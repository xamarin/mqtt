using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
    /// <summary>
    /// The exception thrown when a protocol violation is caused
    /// </summary>
	[DataContract]
	public class MqttProtocolViolationException : MqttException
	{
		public MqttProtocolViolationException ()
		{
		}

		public MqttProtocolViolationException (string message) : base (message)
		{
		}

		public MqttProtocolViolationException (string message, Exception innerException) : base (message, innerException)
		{
		}
	}
}
