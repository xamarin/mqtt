using MqttClient = System.Net.Mqtt.Bindings;

namespace System.Net.Mqtt.Server.Bindings
{
    public class TcpBinding : MqttClient.TcpBinding, IMqttServerBinding
    {
        public IMqttChannelProvider GetChannelProvider (MqttConfiguration configuration)
        {
            return new TcpChannelProvider (configuration);
        }
    }
}
