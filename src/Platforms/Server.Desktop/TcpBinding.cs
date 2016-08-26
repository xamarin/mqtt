using System.Net.Mqtt.Diagnostics;

namespace System.Net.Mqtt.Server
{
	public class TcpBinding : IMqttBinding
	{
		public IMqttChannelFactory GetChannelFactory (string hostAddress, ITracerManager tracerManager, MqttConfiguration configuration)
		{
			return new TcpChannelFactory (hostAddress, tracerManager, configuration);
		}

		public IMqttChannelProvider GetChannelProvider (ITracerManager tracerManager, MqttConfiguration configuration)
		{
			return new TcpChannelProvider (tracerManager, configuration);
		}
	}
}
