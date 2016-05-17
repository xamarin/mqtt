using System.Net.Mqtt.Diagnostics;

namespace System.Net.Mqtt
{
	public class TcpBinding : IProtocolBinding
	{
		public IChannelFactory GetChannelFactory(string hostAddress, ITracerManager tracerManager, ProtocolConfiguration configuration)
		{
			throw new NotImplementedException();
		}

		public IChannelProvider GetChannelProvider(ITracerManager tracerManager, ProtocolConfiguration configuration)
		{
			throw new NotImplementedException();
		}
	}
}
