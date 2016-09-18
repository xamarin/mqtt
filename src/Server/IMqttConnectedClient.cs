using System.ComponentModel;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Packets;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents an <see cref="IMqttClient"/> that has already
    /// performed the protocol connection
    /// This interface is only provided by the Server when creating in process clients
    /// doing <see cref="IMqttServer.CreateClientAsync" /> 
    /// </summary>
    public interface IMqttConnectedClient : IMqttClient
    {
        /// <summary>
        /// Represents the protocol connection, which consists of sending a CONNECT packet
        /// and awaiting the corresponding CONNACK packet from the Server
        /// </summary>
        /// <param name="credentials">
        /// The credentials used to connect to the Server. 
        /// See <see cref="MqttClientCredentials" /> for more details on the credentials information
        /// </param>
        /// <param name="will">
        /// The last will message to send from the Server when an unexpected Client disconnection occurrs. 
        /// See <see cref="MqttLastWill" /> for more details about the will message structure
        /// </param>
        /// <param name="cleanSession">
        /// Indicates if the session state between Client and Server must be cleared between connections
        /// Defaults to false, meaning that session state will be preserved by default accross connections
        /// </param>
        /// <exception cref="MqttClientException">MqttClientException</exception>
        /// <remarks>
        /// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180841">MQTT Connect</a>
        /// for more details about the protocol connection
        /// </remarks>
        [EditorBrowsable (EditorBrowsableState.Never)]
        new Task ConnectAsync(MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false);
    }
}
