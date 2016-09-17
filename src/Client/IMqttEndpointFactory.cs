using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Provides a factory for creating MQTT Server or Client endpoints
    /// </summary>
    /// <typeparam name="T">The type of endpoint to create, being this a Server or a Client</typeparam>
    /// <remarks>
    /// This is the interface exposed when a Server or Client wants to be created.
    /// Given that no Server or Client classes are exposed directly, but only through the corresponding interfaces,
    /// the only way of getting them instantiated is through the specific Client or Server factories
    /// </remarks>
	public interface IMqttEndpointFactory<T>
	{
        /// <summary>
        /// Creates an endpoint of a specified type
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for creating the endpoint
        /// See <see cref="MqttConfiguration" /> for more details about the supported values
        /// </param>
        /// <returns>An endpoint of the specified type</returns>
		Task<T> CreateAsync (MqttConfiguration configuration);
	}
}
