using System.Net.Sockets;
using Hermes.Flows;
using Hermes.Storage;

namespace Hermes
{
	public class ClientInitializer : IInitalizer<Client>
	{
		readonly string hostAddress;

		public ClientInitializer (string hostAddress)
		{
			this.hostAddress = hostAddress;
		}

		public Client Initialize (ProtocolConfiguration configuration)
		{
			var tcpClient = new TcpClient();

			tcpClient.Connect (this.hostAddress, configuration.Port);

			var buffer = new PacketBuffer();
			var channel = new TcpChannel(tcpClient, buffer, configuration);
			var topicEvaluator = new TopicEvaluator(configuration);
			var channelFactory = new PacketChannelFactory(topicEvaluator);
			var repositoryProvider = new InMemoryRepositoryProvider();
			var flowProvider = new ClientProtocolFlowProvider(topicEvaluator, repositoryProvider, configuration);
			var channelAdapter = new ClientPacketChannelAdapter(flowProvider, configuration);

			return new Client (channel, channelFactory, channelAdapter, flowProvider, repositoryProvider, configuration);
		}
	}
}
