using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
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
