using System.Diagnostics;
using System.Net.Mqtt.Sdk.Bindings;
using System.Net.Mqtt.Sdk.Flows;
using System.Net.Mqtt.Sdk.Storage;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk
{
	class MqttConnectedClientFactory
	{
		static readonly ITracer tracer = Tracer.Get<MqttClientFactory>();

		readonly ISubject<PrivateStream> privateStreamListener;

		public MqttConnectedClientFactory(ISubject<PrivateStream> privateStreamListener)
		{
			this.privateStreamListener = privateStreamListener;
		}

		public async Task<IMqttConnectedClient> CreateClientAsync(MqttConfiguration configuration)
		{
			try
			{
				//Adding this to not break backwards compatibility related to the method signature
				//Yielding at this point will cause the method to return immediately after it's called,
				//running the rest of the logic acynchronously
				await Task.Yield();

				var binding = new PrivateBinding(privateStreamListener, EndpointIdentifier.Client);
				var topicEvaluator = new MqttTopicEvaluator(configuration);
				var innerChannelFactory = binding.GetChannelFactory(IPAddress.Loopback.ToString(), configuration);
				var channelFactory = new PacketChannelFactory(innerChannelFactory, topicEvaluator, configuration);
				var packetIdProvider = new PacketIdProvider();
				var repositoryProvider = new InMemoryRepositoryProvider();
				var flowProvider = new ClientProtocolFlowProvider(topicEvaluator, repositoryProvider, configuration);

				return new MqttConnectedClient(channelFactory, flowProvider, repositoryProvider, packetIdProvider, configuration);
			}
			catch (Exception ex)
			{
				tracer.Error(ex, Properties.Resources.Client_InitializeError);

				throw new MqttClientException(Properties.Resources.Client_InitializeError, ex);
			}
		}
	}

	class MqttConnectedClient : MqttClientImpl, IMqttConnectedClient
	{
		internal MqttConnectedClient(IPacketChannelFactory channelFactory,
			IProtocolFlowProvider flowProvider,
			IRepositoryProvider repositoryProvider,
			IPacketIdProvider packetIdProvider,
			MqttConfiguration configuration)
			: base(channelFactory, flowProvider, repositoryProvider, packetIdProvider, configuration)
		{
		}
	}
}
