using System.Net.Mqtt.Bindings;
using System.Net.Mqtt.Diagnostics;

namespace System.Net.Mqtt.Server.Bindings
{
    public interface IMqttServerBinding : IMqttBinding
    {
        IMqttChannelProvider GetChannelProvider (ITracerManager tracerManager, MqttConfiguration configuration);
    }
}
