namespace System.Net.Mqtt.Sdk
{
    /// <summary>
    /// Represents a listener for incoming channel connections on top of an underlying transport protocol
    /// The connections are performed by MQTT Clients through <see cref="IMqttChannel{T}" /> 
    /// and the listener accepts and establishes the connection on the Server side
    /// </summary>
	public interface IMqttChannelListener : IDisposable
	{
        /// <summary>
        /// Provides the stream of incoming channels on top of an underlying transport protocol
        /// </summary>
        /// <returns>An observable sequence of <see cref="IMqttChannel{T}"/> of byte[]</returns>
		IObservable<IMqttChannel<byte[]>> GetChannelStream ();
	}
}
