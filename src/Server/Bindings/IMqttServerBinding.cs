namespace System.Net.Mqtt.Bindings
{
    public interface IMqttServerBinding : IMqttBinding
    {
        IMqttChannelListener GetChannelListener (MqttConfiguration configuration);
    }
}
