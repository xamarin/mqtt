using System.Net.Mqtt.Diagnostics;
using Client = System.Net.Mqtt.Bindings;

namespace System.Net.Mqtt.Server.Bindings
{
    public class TcpBinding : Mqtt.Bindings.TcpBinding, IMqttServerBinding
    {
        public IMqttChannelProvider GetChannelProvider (ITracerManager tracerManager, MqttConfiguration configuration)
        {
            return new TcpChannelProvider (tracerManager, configuration);
        }
    }
}
