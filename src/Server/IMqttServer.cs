using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    public interface IMqttServer : IDisposable
    {
        event EventHandler<MqttUndeliveredMessage> MessageUndelivered;

        event EventHandler<MqttEndpointDisconnected> Stopped;

        int ActiveChannels { get; }

        IEnumerable<string> ActiveClients { get; }

        void Start ();

        Task<IMqttClient> CreateClientAsync ();

        void Stop ();
    }
}
