using System.Net.Mqtt.Packets;
using System.Runtime.Serialization;

namespace System.Net.Mqtt.Exceptions
{
    /// <summary>
    /// The exception that is thrown when something related to the Client connection fails
    /// </summary>
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

        /// <summary>
        /// Code that represents the status and reason of the failure
        /// See <see cref="MqttConnectionStatus" /> for more information about the possible connection status values 
        /// </summary>
		public MqttConnectionStatus ReturnCode { get; set; }
	}
}
