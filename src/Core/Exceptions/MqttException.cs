using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
	[Serializable]
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

		protected MqttException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}
	}
}
