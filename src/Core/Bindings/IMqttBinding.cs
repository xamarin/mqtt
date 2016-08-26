using System.Net.Mqtt.Diagnostics;

namespace System.Net.Mqtt.Bindings
{
	public interface IMqttBinding
	{
		IMqttChannelFactory GetChannelFactory (string hostAddress, ITracerManager tracerManager, MqttConfiguration configuration);
	}
}
