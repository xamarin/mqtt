using System.Net.Mqtt.Bindings;

namespace System.Net.Mqtt.Server.Bindings
{
    public interface IMqttServerBinding : IMqttBinding
    {
        IMqttChannelProvider GetChannelProvider (MqttConfiguration configuration);
    }
}
