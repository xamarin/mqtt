using System.Net.Mqtt.Bindings;

namespace System.Net.Mqtt.Server.Bindings
{
    public interface IMqttServerBinding : IMqttBinding
    {
        IMqttChannelListener GetChannelListener (MqttConfiguration configuration);
    }
}
