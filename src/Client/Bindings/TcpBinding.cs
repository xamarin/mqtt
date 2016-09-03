namespace System.Net.Mqtt.Bindings
{
    public class TcpBinding : IMqttBinding
	{
		public IMqttChannelFactory GetChannelFactory (string hostAddress, MqttConfiguration configuration)
		{
			return new TcpChannelFactory (hostAddress, configuration);
		}
	}
}
