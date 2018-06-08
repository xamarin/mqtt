using System.Net.Mqtt.Sdk;
using System.Net.Mqtt.Sdk.Bindings;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	/// <summary>
	/// Creates instances of <see cref="IMqttClient"/> for connecting to 
	/// an MQTT server.
	/// </summary>
	public static class MqttClient
	{
		/// <summary>
		/// Creates an <see cref="IMqttClient"/> and connects it to the destination 
		/// <paramref name="hostAddress"/> server using the specified transport binding 
		/// and MQTT configuration to customize the protocol parameters.
		/// </summary>
		/// <param name="hostAddress">
		/// Host address to use for the connection
		/// </param>
		/// <param name="configuration">
		/// The configuration used for creating the Client.
		/// See <see cref="MqttConfiguration" /> for more details about the supported values.
		/// </param>
		/// <param name="binding">
		/// The binding to use as the underlying transport layer.
		/// Deafault value: <see cref="TcpBinding"/>
		/// Possible values: <see cref="TcpBinding"/>, <see cref="WebSocketBinding"/>
		/// See <see cref="IMqttBinding"/> for more details about how 
		/// to implement a custom binding
		/// </param>
		/// <returns>A new MQTT Client</returns>
		public static Task<IMqttClient> CreateAsync (string hostAddress, MqttConfiguration configuration, IMqttBinding binding = null) =>
			new MqttClientFactory (hostAddress, binding ?? new TcpBinding ()).CreateClientAsync (configuration);

		/// <summary>
		/// Creates an <see cref="IMqttClient"/> and connects it to the destination 
		/// <paramref name="hostAddress"/> server via TCP using the specified port.
		/// </summary>
		/// <returns>A new MQTT Client</returns>
		public static Task<IMqttClient> CreateAsync (string hostAddress, int port) => 
			new MqttClientFactory (hostAddress).CreateClientAsync (new MqttConfiguration { Port = port });

		/// <summary>
		/// Creates an <see cref="IMqttClient"/> and connects it to the destination 
		/// <paramref name="hostAddress"/> server via TCP using the protocol defaults.
		/// </summary>
		/// <returns>A new MQTT Client</returns>
		public static Task<IMqttClient> CreateAsync (string hostAddress) =>
			new MqttClientFactory (hostAddress).CreateClientAsync (new MqttConfiguration ());

		internal static string GetPrivateClientId () =>
			string.Format (
				"private{0}",
				Guid.NewGuid ().ToString ().Replace ("-", string.Empty).Substring (0, 10)
			);

		internal static string GetAnonymousClientId () =>
			string.Format (
				"anonymous{0}",
				Guid.NewGuid ().ToString ().Replace ("-", string.Empty).Substring (0, 10)
			);
	}
}
