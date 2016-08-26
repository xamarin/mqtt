using System.Net.Mqtt.Diagnostics;

namespace System.Net.Mqtt.Server
{
	public class TcpBinding : IProtocolBinding
	{
		public IChannelFactory GetChannelFactory (string hostAddress, ITracerManager tracerManager, ProtocolConfiguration configuration)
		{
			return new TcpChannelFactory (hostAddress, tracerManager, configuration);
		}

		public IChannelProvider GetChannelProvider (ITracerManager tracerManager, ProtocolConfiguration configuration)
		{
			return new TcpChannelProvider (tracerManager, configuration);
		}
	}
}
