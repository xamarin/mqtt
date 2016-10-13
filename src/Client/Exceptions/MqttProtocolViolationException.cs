using System.Runtime.Serialization;

namespace System.Net.Mqtt
{
    /// <summary>
    /// The exception thrown when a protocol violation is caused
    /// </summary>
	[DataContract]
	public class MqttProtocolViolationException : MqttException
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttProtocolViolationException" /> class
        /// </summary>
		public MqttProtocolViolationException ()
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttProtocolViolationException" /> class,
        /// using the specified error message
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
		public MqttProtocolViolationException (string message) : base (message)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttProtocolViolationException" /> class,
        /// with a specified error message and a reference to the inner exception that is the cause
        /// of this exception
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
		public MqttProtocolViolationException (string message, Exception innerException) : base (message, innerException)
		{
		}
	}
}
