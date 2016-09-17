using System.ComponentModel;
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
        [EditorBrowsable (EditorBrowsableState.Never)]
        new Task ConnectAsync(MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false);
    }
}
