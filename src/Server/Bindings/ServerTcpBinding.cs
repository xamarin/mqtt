namespace System.Net.Mqtt.Bindings
{
    public class ServerTcpBinding : TcpBinding, IMqttServerBinding
    {
        public IMqttChannelListener GetChannelListener (MqttConfiguration configuration)
        {
            return new TcpChannelListener (configuration);
        }
    }
}
