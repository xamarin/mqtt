using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Provides a factory for creating channels of byte[]
    /// See <see cref="IMqttChannel{T}" /> to know more about channel capabilities
    /// </summary>
	public interface IMqttChannelFactory
	{
        /// <summary>
        /// Creates instances of <see cref="IMqttChannel{T}"/> of byte[]
        /// </summary>
        /// <returns>An MQTT channel of byte[]</returns>
		Task<IMqttChannel<byte[]>> CreateAsync ();
	}
}
