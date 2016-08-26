using System.Net.Mqtt.Packets;
using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
	[DataContract]
	public class MqttConnectionException : MqttException
	{
		public MqttConnectionException (MqttConnectionStatus status)
		{
			ReturnCode = status;
		}

		public MqttConnectionException (MqttConnectionStatus status, string message) : base (message)
		{
			ReturnCode = status;
		}

		public MqttConnectionException (MqttConnectionStatus status, string message, Exception innerException) : base (message, innerException)
		{
			ReturnCode = status;
		}

		public MqttConnectionStatus ReturnCode { get; set; }
	}
}
