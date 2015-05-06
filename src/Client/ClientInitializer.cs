using System.Net.Sockets;
using Hermes.Diagnostics;
using Hermes.Flows;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes
{
	public class ClientInitializer : IInitalizer<Client>
	{
		private static readonly ITracer tracer = Tracer.Get<ClientInitializer> ();

		private readonly string hostAddress;

		public ClientInitializer (string hostAddress)
		{
			this.hostAddress = hostAddress;
		}

		/// <exception cref="ClientException">ClientException</exception>
		public Client Initialize (ProtocolConfiguration configuration)
		{
			Tracing.Initialize (configuration);

			var tcpClient = new TcpClient();

			try {
				tcpClient.Connect (this.hostAddress, configuration.Port);
			} catch (SocketException socketEx) {
				var message = string.Format(Resources.Client_TcpClient_Failed, this.hostAddress, configuration.Port);

				tracer.Error (socketEx, message);

				throw new ClientException(message, socketEx);
			}

			var buffer = new PacketBuffer();
			var channel = new TcpChannel(tcpClient, buffer, configuration);
			var topicEvaluator = new TopicEvaluator(configuration);
			var channelFactory = new PacketChannelFactory(topicEvaluator, configuration);
			var repositoryProvider = new InMemoryRepositoryProvider();
			var flowProvider = new ClientProtocolFlowProvider(topicEvaluator, repositoryProvider, configuration);
			var packetListener = new ClientPacketListener(flowProvider, configuration);

			return new Client (channel, channelFactory, packetListener, flowProvider, repositoryProvider, configuration);
		}
	}
}
