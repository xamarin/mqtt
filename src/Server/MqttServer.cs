using System.Net.Mqtt.Sdk;
using System.Net.Mqtt.Sdk.Bindings;

namespace System.Net.Mqtt
{
	/// <summary>
	/// Creates instances of <see cref="IMqttServer"/> for accepting connections from 
	/// MQTT clients.
	/// </summary>
	public static class MqttServer
	{
		/// <summary>
		/// Creates an <see cref="IMqttServer"/> using the specified MQTT configuration 
		/// to customize the protocol parameters, and an optional transport binding and authentication provider.
		/// </summary>
		/// <param name="configuration">
		/// The configuration used for creating the Server.
		/// See <see cref="MqttConfiguration" /> for more details about the supported values.
		/// </param>
		/// <param name="binding">
		/// The binding to use as the underlying transport layer.
		/// Possible values: <see cref="ServerTcpBinding"/>, <see cref="ServerWebSocketBinding"/>
		/// See <see cref="IMqttServerBinding"/> for more details about how 
		/// to implement a custom binding
		/// </param>
		/// <param name="authenticationProvider">
		/// Optional authentication provider to use, 
		/// to enable authentication as part of the connection mechanism.
		/// See <see cref="IMqttAuthenticationProvider" /> for more details about how to implement
		/// an authentication provider.
		/// </param>
		/// <returns>A new MQTT Server</returns>
		/// <exception cref="MqttServerException">MqttServerException</exception>
		public static IMqttServer Create(MqttConfiguration configuration, IMqttServerBinding binding, IMqttAuthenticationProvider authenticationProvider = null)
			=> new MqttServerFactory(binding, authenticationProvider).CreateServer(configuration);

		/// <summary>
		/// Creates an <see cref="IMqttServer"/> using a TCP binding and a specified MQTT configuration.
		/// </summary>
		/// <param name="configuration">
		/// The configuration used for creating the Server.
		/// See <see cref="MqttConfiguration" /> for more details about the supported values.
		/// </param>
		/// <returns>A new MQTT Server</returns>
		/// <exception cref="MqttServerException">MqttServerException</exception>
		public static IMqttServer CreateTcp(MqttConfiguration configuration) => new MqttServerFactory(new ServerTcpBinding()).CreateServer(configuration);

		/// <summary>
		/// Creates an <see cref="IMqttServer"/> using a TCP binding and a specified port.
		/// </summary>
		/// <param name="port">
		/// The port to listen for incoming connections.
		/// </param>
		/// <returns>A new MQTT Server</returns>
		/// <exception cref="MqttServerException">MqttServerException</exception>
		public static IMqttServer CreateTcp(int port) => new MqttServerFactory(new ServerTcpBinding()).CreateServer(new MqttConfiguration { Port = port });

		/// <summary>
		/// Creates an <see cref="IMqttServer"/> using a TCP binding and the MQTT protocol defaults.
		/// </summary>
		/// <returns>A new MQTT Server</returns>
		/// <exception cref="MqttServerException">MqttServerException</exception>
		public static IMqttServer CreateTcp() => new MqttServerFactory(new ServerTcpBinding()).CreateServer(new MqttConfiguration());

		/// <summary>
		/// Creates an <see cref="IMqttServer"/> using an in-memory binding and a specified MQTT configuration.
		/// </summary>
		/// <param name="configuration">
		/// The configuration used for creating the Server.
		/// See <see cref="MqttConfiguration" /> for more details about the supported values.
		/// </param>
		/// <returns>A new MQTT Server</returns>
		/// <exception cref="MqttServerException">MqttServerException</exception>
		public static IMqttServer CreateInMemory(MqttConfiguration configuration) => new MqttServerFactory().CreateServer(configuration);

		/// <summary>
		/// Creates an <see cref="IMqttServer"/> using an in-memory binding and the MQTT protocol defaults.
		/// </summary>
		/// <returns>A new MQTT Server</returns>
		/// <exception cref="MqttServerException">MqttServerException</exception>
		public static IMqttServer CreateInMemory() => new MqttServerFactory().CreateServer(new MqttConfiguration());
	}
}
