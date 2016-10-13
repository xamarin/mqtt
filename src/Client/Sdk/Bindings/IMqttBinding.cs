namespace System.Net.Mqtt.Sdk.Bindings
{
	/// <summary>
	/// Represents a binding for a supported MQTT underlying transport protocol
	/// </summary>
	/// <remarks>
	/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180911">Network Connections</a>
	/// for more details about default and supported transport protocols for MQTT
	/// </remarks>
	public interface IMqttBinding
	{
        /// <summary>
        /// Provides a factory for MQTT channels on top of an underlying transport protocol
        /// See <see cref="IMqttChannelFactory" /> for more details about the factory 
        /// </summary>
        /// <param name="hostAddress">Host name or IP address to connect the channels</param>
        /// <param name="configuration">
        /// The configuration used for creating the factory and channels
        /// See <see cref="MqttConfiguration" /> for more details about the supported values
        /// </param>
        /// <returns>A factory for creating MQTT channels on top of an underlying transport protocol</returns>
		IMqttChannelFactory GetChannelFactory (string hostAddress, MqttConfiguration configuration);
	}
}
