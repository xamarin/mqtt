using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Provides a factory for MQTT endpoints of a specified type
    /// </summary>
    /// <typeparam name="T">The type of endpoint to create</typeparam>
	public interface IMqttEndpointFactory<T>
	{
        /// <summary>
        /// Creates an endpoint of a specified type
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for creating the endpoint
        /// See <see cref="MqttConfiguration" /> for more details about the supported values
        /// </param>
        /// <returns>An endpoint of the specified type T</returns>
		Task<T> CreateAsync (MqttConfiguration configuration);
	}
}
