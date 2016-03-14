using System.Runtime.Serialization;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Exceptions
{
	[Serializable]
	public class MqttConnectionException : MqttException
	{
		public MqttConnectionException (ConnectionStatus status)
		{
			ReturnCode = status;
		}

		public MqttConnectionException (ConnectionStatus status, string message) : base (message)
		{
			ReturnCode = status;
		}

		public MqttConnectionException (ConnectionStatus status, string message, Exception innerException) : base (message, innerException)
		{
			ReturnCode = status;
		}

		protected MqttConnectionException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}

		public ConnectionStatus ReturnCode { get; set; }
	}
}
