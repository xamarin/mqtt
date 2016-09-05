using System.Diagnostics;
using System.Net.Mqtt.Bindings;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Storage;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    public class MqttClientFactory : IMqttEndpointFactory<IMqttClient>
	{
		static readonly ITracer tracer = Tracer.Get<MqttClientFactory> ();

		readonly string hostAddress;
		readonly IMqttBinding binding;

        public MqttClientFactory (string hostAddress)
            : this (hostAddress, new TcpBinding ())
        {
        }

        public MqttClientFactory (string hostAddress, IMqttBinding binding)
        {
            this.hostAddress = hostAddress;
            this.binding = binding;
        }

        /// <exception cref="MqttClientException">ClientException</exception>
        public async Task<IMqttClient> CreateAsync (MqttConfiguration configuration)
		{
			try {
				var topicEvaluator = new MqttTopicEvaluator (configuration);
				var innerChannelFactory = binding.GetChannelFactory (hostAddress, configuration);
				var channelFactory = new PacketChannelFactory (innerChannelFactory, topicEvaluator, configuration);
				var packetIdProvider = new PacketIdProvider ();
				var repositoryProvider = new InMemoryRepositoryProvider ();
				var flowProvider = new ClientProtocolFlowProvider (topicEvaluator, repositoryProvider, configuration);
				var packetChannel = await channelFactory
                    .CreateAsync ()
                    .ConfigureAwait (continueOnCapturedContext: false);

				return new Client (packetChannel, flowProvider, repositoryProvider, packetIdProvider, configuration);
			} catch (Exception ex) {
				tracer.Error (ex, Properties.Resources.Client_InitializeError);

				throw new MqttClientException  (Properties.Resources.Client_InitializeError, ex);
			}
		}
	}
}
