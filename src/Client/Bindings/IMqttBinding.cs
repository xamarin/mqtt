namespace System.Net.Mqtt.Bindings
{
    public interface IMqttBinding
	{
		IMqttChannelFactory GetChannelFactory (string hostAddress, MqttConfiguration configuration);
	}
}
