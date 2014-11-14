using System.Reactive;
using Hermes.Storage;
using System.Reactive.Subjects;

namespace Hermes
{
	public class ClientFactory : IClientFactory
	{
		readonly string serverAddress;
		readonly IProtocolConfiguration configuration;
		readonly ISocketFactory socketFactory;

		public ClientFactory (string serverAddress, IProtocolConfiguration configuration, ISocketFactory socketFactory)
		{
			this.serverAddress = serverAddress;
			this.configuration = configuration;
			this.socketFactory = socketFactory;
		}

		public IClient CreateClient ()
		{
			var socket = this.socketFactory.CreateSocket (this.serverAddress);
			var topicEvaluator = new TopicEvaluator();
			var packetChannelFactory = new PacketChannelFactory (topicEvaluator); //TODO: We need to inject this, but it currently affects Client experience creating factories
			var channel = packetChannelFactory.CreateChannel (socket);
			//var time = new Su
			var packetIdentifierRepository = new InMemoryRepository<PacketIdentifier>(); //TODO: We need to inject this, but it currently affects Client experience creating factories

			return default (IClient); //new Client (channel, this.configuration, packetIdentifierRepository);
		}
	}
}
