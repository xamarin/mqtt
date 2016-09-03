using System.Collections.Generic;

namespace System.Net.Mqtt.Server
{
    public interface IMqttServer : IDisposable
    {
        event EventHandler<MqttUndeliveredMessage> MessageUndelivered;

        event EventHandler<MqttEndpointDisconnected> Stopped;

        int ActiveChannels { get; }

        IEnumerable<string> ActiveClients { get; }

        void Start ();

        void Stop ();
    }
}
