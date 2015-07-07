using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
	[Serializable]
	public class MqttViolationException : MqttException
	{
		public MqttViolationException ()
		{
		}

		public MqttViolationException (string message) : base(message)
		{
		}

		public MqttViolationException (string message, Exception innerException) : base(message, innerException)
		{
		}

		protected MqttViolationException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}
	}
}
