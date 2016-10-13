namespace System.Net.Mqtt.Sdk.Bindings
{
	/// <summary>
	/// Represents a server binding for a supported MQTT underlying transport protocol
	/// </summary>
	/// <remarks>
	/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180911">Network Connections</a>
	/// for more details about default and supported transport protocols for MQTT
	/// </remarks>
	public interface IMqttServerBinding : IMqttBinding
    {
        /// <summary>
        /// Provides a listener for incoming MQTT channels on top of an underlying transport protocol
        /// See <see cref="IMqttChannelListener" /> for more details about the listener 
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for creating the listener
        /// See <see cref="MqttConfiguration" /> for more details about the supported values
        /// </param>
        /// <returns>A listener to accept and provide incoming MQTT channels on top of an underlying transport protocol</returns>
        IMqttChannelListener GetChannelListener (MqttConfiguration configuration);
    }
}
