using System.ComponentModel;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	/// <summary>
	/// Represents an <see cref="IMqttClient"/> that has already
	/// performed the protocol connection
	/// This interface is only provided by the Server when creating in process clients
	/// doing <see cref="IMqttServer.CreateClientAsync" />, since invoking the 
	/// <see cref="IMqttClient.ConnectAsync(MqttClientCredentials, MqttLastWill, bool)"/> 
	/// is not necessary.
	/// </summary>
	public interface IMqttConnectedClient : IMqttClient
    {
        /// <summary>
        /// Hides the unnecessary <see cref="IMqttClient.ConnectAsync(MqttClientCredentials, MqttLastWill, bool)"/> 
		/// method from the interface.
        /// </summary>
        [EditorBrowsable (EditorBrowsableState.Never)]
        new Task<SessionState> ConnectAsync(MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false);
    }
}
