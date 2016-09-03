using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Exceptions
{
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
