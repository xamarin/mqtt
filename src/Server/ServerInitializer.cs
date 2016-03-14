using System.Reactive;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Storage;

namespace System.Net.Mqtt.Server
{
	public class ServerInitializer : IInitalizer<Server>
	{
		readonly IAuthenticationProvider authenticationProvider;
		readonly IChannelProvider channelProvider;

		public ServerInitializer (IAuthenticationProvider authenticationProvider = null, IChannelProvider channelProvider = null)
		{
			this.authenticationProvider = authenticationProvider ?? NullAuthenticationProvider.Instance;
			this.channelProvider = channelProvider;
		}

		public Server Initialize (ProtocolConfiguration configuration)
		{
			var topicEvaluator = new TopicEvaluator (configuration);
			var channelFactory = new PacketChannelFactory (topicEvaluator, configuration);
			var repositoryProvider = new InMemoryRepositoryProvider ();
			var connectionProvider = new ConnectionProvider ();
			var packetIdProvider = new PacketIdProvider ();
			var eventStream = new EventStream ();
			var flowProvider = new ServerProtocolFlowProvider (authenticationProvider, connectionProvider, topicEvaluator,
				repositoryProvider, packetIdProvider, eventStream, configuration);

			//TODO: The ChannelProvider injection must be handled better. I would not assume Tcp by default. 
			//Instead I would delegate the implementation to a different NuGet or assembly.
			//Maybe having one assembly per provider implementation (like TcpChannelProvider, WebSocketChannelProvider, TLSChannelProvider, etc)
			return new Server (channelProvider ?? new TcpChannelProvider (configuration), channelFactory,
				flowProvider, connectionProvider, eventStream, configuration);
		}
	}
}
