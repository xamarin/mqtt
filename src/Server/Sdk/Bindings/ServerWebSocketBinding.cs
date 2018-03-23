namespace System.Net.Mqtt.Sdk.Bindings
{
	/// <summary>
	/// Server binding to use Web Sockets as the underlying MQTT transport protocol
	/// </summary>
	public class ServerWebSocketBinding : WebSocketBinding, IMqttServerBinding
    {
        /// <summary>
        /// Provides a listener for incoming MQTT channels on top of Web Sockets
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for creating the listener
        /// See <see cref="MqttConfiguration" /> for more details about the supported values
        /// </param>
        /// <returns>A listener to accept and provide incoming MQTT channels on top of Web Sockets</returns>
        public IMqttChannelListener GetChannelListener (MqttConfiguration configuration) 
			=> new WebSocketChannelListener (configuration);
    }
}
