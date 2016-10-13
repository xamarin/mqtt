using System.Runtime.Serialization;

namespace System.Net.Mqtt
{
	/// <summary>
	/// The exception that is thrown when something related to the Client connection fails
	/// </summary>
	[DataContract]
	public class MqttConnectionException : MqttException
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttConnectionException" /> class,
        /// specifying the status and reason of the connection failure
        /// </summary>
        /// <param name="status">
        /// Code that represents the status and reason of the failure
        /// See <see cref="MqttConnectionStatus" /> for more information about the possible connection status values 
        /// </param>
        public MqttConnectionException (MqttConnectionStatus status)
		{
			ReturnCode = status;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttConnectionException" /> class,
        /// specifying the status and reason of the connection failure 
        /// and using the specified error message
        /// </summary>
        /// <param name="status">
        /// Code that represents the status and reason of the failure
        /// See <see cref="MqttConnectionStatus" /> for more information about the possible connection status values 
        /// </param>
        /// <param name="message">The error message that explains the reason for the exception</param>
		public MqttConnectionException (MqttConnectionStatus status, string message) : base (message)
		{
			ReturnCode = status;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttConnectionException" /> class,
        /// specifying the status and reason of the connection failure , 
        /// a specific error message and a reference to the inner exception that is the cause
        /// of this exception
        /// </summary>
        /// <param name="status">
        /// Code that represents the status and reason of the failure
        /// See <see cref="MqttConnectionStatus" /> for more information about the possible connection status values 
        /// </param>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
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
