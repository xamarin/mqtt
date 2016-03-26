using System.Net.Mqtt.Diagnostics;

namespace System.Net.Mqtt
{
	public interface IProtocolBinding
	{
		IChannelFactory GetChannelFactory (string hostAddress, ITracerManager tracerManager, ProtocolConfiguration configuration);

		IChannelProvider GetChannelProvider (ITracerManager tracerManager, ProtocolConfiguration configuration);
	}
}
