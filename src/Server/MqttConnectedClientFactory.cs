using System.Diagnostics;
using System.Net.Mqtt.Bindings;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    class MqttConnectedClientFactory : IMqttEndpointFactory<IMqttConnectedClient>
    {
        static readonly ITracer tracer = Tracer.Get<MqttClientFactory>();

        readonly ISubject<PrivateStream> privateStreamListener;

        public MqttConnectedClientFactory (ISubject<PrivateStream> privateStreamListener)
        {
            this.privateStreamListener = privateStreamListener;
        }

        public async Task<IMqttConnectedClient> CreateAsync (MqttConfiguration configuration)
        {
            try
            {
                var binding = new PrivateBinding (privateStreamListener, EndpointIdentifier.Client);
                var topicEvaluator = new MqttTopicEvaluator (configuration);
                var innerChannelFactory = binding.GetChannelFactory (IPAddress.Loopback.ToString (), configuration);
                var channelFactory = new PacketChannelFactory (innerChannelFactory, topicEvaluator, configuration);
                var packetIdProvider = new PacketIdProvider ();
                var repositoryProvider = new InMemoryRepositoryProvider ();
                var flowProvider = new ClientProtocolFlowProvider (topicEvaluator, repositoryProvider, configuration);
                var packetChannel = await channelFactory
                    .CreateAsync ()
                    .ConfigureAwait (continueOnCapturedContext: false);

                return new MqttConnectedClient (packetChannel, flowProvider, repositoryProvider, packetIdProvider, configuration);
            }
            catch (Exception ex)
            {
                tracer.Error(ex, Properties.Resources.Client_InitializeError);

                throw new MqttClientException (Properties.Resources.Client_InitializeError, ex);
            }
        }
    }

    class MqttConnectedClient : MqttClient, IMqttConnectedClient
    {
        internal MqttConnectedClient(IMqttChannel<IPacket> packetChannel,
            IProtocolFlowProvider flowProvider,
            IRepositoryProvider repositoryProvider,
            IPacketIdProvider packetIdProvider,
            MqttConfiguration configuration)
            : base(packetChannel, flowProvider, repositoryProvider, packetIdProvider, configuration)
        {
        }
    }
}
