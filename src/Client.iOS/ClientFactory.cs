using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Storage;

namespace System.Net.Mqtt.Client
{
    public class ClientFactory : IFactory<Client>
    {
        readonly ITracerManager tracerManager;
        readonly ITracer tracer;
        readonly string hostAddress;
        readonly IProtocolBinding binding;

        public ClientFactory (string hostAddress, IProtocolBinding binding)
            : this (hostAddress, binding, new DefaultTracerManager ())
        {
        }

        public ClientFactory (string hostAddress, IProtocolBinding binding, ITracerManager tracerManager)
        {
            tracer = tracerManager.Get<ClientFactory> ();
            this.tracerManager = tracerManager;
            this.hostAddress = hostAddress;
            this.binding = binding;
        }

        ///// <exception cref="ClientException">ClientException</exception>
        public Client Create (ProtocolConfiguration configuration)
        {
            try {
                var topicEvaluator = new TopicEvaluator (configuration);
                var innerChannelFactory = binding.GetChannelFactory (hostAddress, tracerManager, configuration);
                var channelFactory = new PacketChannelFactory (innerChannelFactory, topicEvaluator, tracerManager, configuration);
                var packetIdProvider = new PacketIdProvider ();
                var repositoryProvider = new InMemoryRepositoryProvider();
                var flowProvider = new ClientProtocolFlowProvider (topicEvaluator, repositoryProvider, tracerManager, configuration);
                var packetChannel = channelFactory.Create ();

                return new Client (packetChannel, flowProvider, repositoryProvider, packetIdProvider, tracerManager, configuration);
            } catch (Exception ex) {
                tracer.Error (ex, Properties.Resources.Client_InitializeError);

                throw new ClientException (Properties.Resources.Client_InitializeError, ex);
            }
        }
    }
}
