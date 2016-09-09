using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
    /// <summary>
    /// The exception thrown when a protocol violation is caused
    /// </summary>
	[DataContract]
	public class MqttViolationException : MqttException
	{
		public MqttViolationException ()
		{
		}

		public MqttViolationException (string message) : base (message)
		{
		}

		public MqttViolationException (string message, Exception innerException) : base (message, innerException)
		{
		}
	}
}
