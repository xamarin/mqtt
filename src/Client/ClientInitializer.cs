using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Storage;

namespace System.Net.Mqtt
{
	public class ClientInitializer : IInitalizer<Client>
	{
		static readonly ITracer tracer = Tracer.Get<ClientInitializer> ();

		readonly string hostAddress;
		readonly IChannelFactory innerChannelFactory;

		public ClientInitializer (string hostAddress, IChannelFactory innerChannelFactory = null)
		{
			this.hostAddress = hostAddress;
			this.innerChannelFactory = innerChannelFactory;
		}

		/// <exception cref="ClientException">ClientException</exception>
		public Client Initialize (ProtocolConfiguration configuration)
		{
			try {
				var topicEvaluator = new TopicEvaluator(configuration);
				//TODO: The ChannelFactory injection must be handled better. I would not assume Tcp by default. 
				//Instead I would delegate the implementation to a different NuGet or assembly.
				//Maybe having one assembly per factory implementation (like TcpChannelFactory, WebSocketChannelFactory, TLSChannelFactory, etc)
				var channelFactory = new PacketChannelFactory(this.innerChannelFactory ?? new TcpChannelFactory (this.hostAddress, configuration), 
					topicEvaluator, configuration);
				var packetIdProvider = new PacketIdProvider ();
				var repositoryProvider = new InMemoryRepositoryProvider();
				var flowProvider = new ClientProtocolFlowProvider(topicEvaluator, repositoryProvider, configuration);
				var packetChannel = channelFactory.Create ();

				return new Client (packetChannel, flowProvider, repositoryProvider, packetIdProvider, configuration);
			} catch (Exception ex) {
				tracer.Error (ex, Properties.Resources.Client_InitializeError);

				throw new ClientException (Properties.Resources.Client_InitializeError, ex);
			}
		}
	}
}
