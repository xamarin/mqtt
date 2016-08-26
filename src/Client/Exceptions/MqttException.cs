using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
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