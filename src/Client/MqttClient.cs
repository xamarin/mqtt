using System.Net.Mqtt.Sdk;
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
		/// <paramref name="hostAddress"/> server via TCP using the specified 
		/// MQTT configuration to customize the protocol parameters.
		/// </summary>
		public static Task<IMqttClient> CreateAsync (string hostAddress, MqttConfiguration configuration) =>
			new MqttClientFactory (hostAddress).CreateClientAsync (configuration);

		/// <summary>
		/// Creates an <see cref="IMqttClient"/> and connects it to the destination 
		/// <paramref name="hostAddress"/> server via TCP using the specified port.
		/// </summary>
		public static Task<IMqttClient> CreateAsync (string hostAddress, int port) => 
			new MqttClientFactory (hostAddress).CreateClientAsync (new MqttConfiguration { Port = port });

		/// <summary>
		/// Creates an <see cref="IMqttClient"/> and connects it to the destination 
		/// <paramref name="hostAddress"/> server via TCP using the protocol defaults.
		/// </summary>
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
