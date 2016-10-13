using System.Runtime.Serialization;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents the base exception for any MQTT failure
    /// </summary>
	[DataContract]
	public class MqttException : Exception
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttException" /> class
        /// </summary>
        public MqttException ()
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttException" /> class,
        /// using the specified error message
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        public MqttException (string message) : base (message)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttException" /> class,
        /// with a specified error message and a reference to the inner exception that is the cause
        /// of this exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public MqttException (string message, Exception innerException) : base (message, innerException)
		{
		}
	}
}