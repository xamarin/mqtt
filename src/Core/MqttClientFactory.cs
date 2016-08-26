using System.Net.Mqtt.Bindings;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Storage;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	public class MqttClientFactory : IMqttEndpointFactory<IMqttClient>
	{
		readonly ITracerManager tracerManager;
		readonly ITracer tracer;
		readonly string hostAddress;
		readonly IMqttBinding binding;

        public MqttClientFactory (string hostAddress, IMqttBinding binding)
            : this (hostAddress, binding, new DefaultTracerManager ())
        {
        }

        public MqttClientFactory (string hostAddress, IMqttBinding binding, ITracerManager tracerManager)
        {
            tracer = tracerManager.Get<MqttClientFactory> ();
            this.tracerManager = tracerManager;
            this.hostAddress = hostAddress;
            this.binding = binding;
        }

        /// <exception cref="MqttClientException">ClientException</exception>
        public async Task<IMqttClient> CreateAsync (MqttConfiguration configuration)
		{
			try {
				var topicEvaluator = new MqttTopicEvaluator (configuration);
				var innerChannelFactory = binding.GetChannelFactory (hostAddress, tracerManager, configuration);
				var channelFactory = new PacketChannelFactory (innerChannelFactory, topicEvaluator, tracerManager, configuration);
				var packetIdProvider = new PacketIdProvider ();
				var repositoryProvider = new InMemoryRepositoryProvider ();
				var flowProvider = new ClientProtocolFlowProvider (topicEvaluator, repositoryProvider, tracerManager, configuration);
				var packetChannel = await channelFactory
                    .CreateAsync ()
                    .ConfigureAwait (continueOnCapturedContext: false);

				return new Client (packetChannel, flowProvider, repositoryProvider, packetIdProvider, tracerManager, configuration);
			} catch (Exception ex) {
				tracer.Error (ex, Resources.Client_InitializeError);

				throw new MqttClientException (Resources.Client_InitializeError, ex);
			}
		}
	}
}
