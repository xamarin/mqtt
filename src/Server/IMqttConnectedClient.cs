using System.ComponentModel;
using System.Net.Mqtt.Packets;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    public interface IMqttConnectedClient : IMqttClient
    {
        [EditorBrowsable (EditorBrowsableState.Never)]
        new Task ConnectAsync(MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false);
    }
}
