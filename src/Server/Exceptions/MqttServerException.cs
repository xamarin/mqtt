using System.Net.Mqtt.Exceptions;

namespace System.Net.Mqtt.Server.Exceptions
{
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
