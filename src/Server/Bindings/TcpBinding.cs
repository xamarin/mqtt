using MqttClient = System.Net.Mqtt.Bindings;

namespace System.Net.Mqtt.Server.Bindings
{
    public class TcpBinding : MqttClient.TcpBinding, IMqttServerBinding
    {
        public IMqttChannelListener GetChannelListener (MqttConfiguration configuration)
        {
            return new TcpChannelListener (configuration);
        }
    }
}
