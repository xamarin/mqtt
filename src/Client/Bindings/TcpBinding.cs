using System.Net.Mqtt.Diagnostics;

namespace System.Net.Mqtt.Bindings
{
	public class TcpBinding : IMqttBinding
	{
		public IMqttChannelFactory GetChannelFactory (string hostAddress, ITracerManager tracerManager, MqttConfiguration configuration)
		{
			return new TcpChannelFactory (hostAddress, tracerManager, configuration);
		}
	}
}
