﻿using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hermes.Flows;
using Hermes.Storage;

namespace Hermes
{
	public class ServerInitializer : IInitalizer<Server>
	{
		public Server Initialize(ProtocolConfiguration configuration)
		{
			Tracing.Initialize (configuration);

			var listener = new TcpListener(IPAddress.Any, configuration.Port);
			var binaryChannelProvider = Observable
				.FromAsync (() => {
					return Task.Factory.FromAsync<TcpClient> (listener.BeginAcceptTcpClient,
						listener.EndAcceptTcpClient, TaskCreationOptions.AttachedToParent);
				})
				.Repeat ()
				.Select (client => new TcpChannel (client, new PacketBuffer (), configuration));
			var channelObservable = new ChannelObservable (listener, binaryChannelProvider);
			var topicEvaluator = new TopicEvaluator (configuration);
			var channelFactory = new PacketChannelFactory (topicEvaluator, configuration);
			var repositoryProvider = new InMemoryRepositoryProvider ();
			var connectionProvider = new ConnectionProvider ();
			var eventStream = new EventStream ();
			var flowProvider = new ServerProtocolFlowProvider (connectionProvider, topicEvaluator, repositoryProvider, eventStream, configuration);

			return new Server (channelObservable, channelFactory, flowProvider, connectionProvider, eventStream, configuration);
		}
	}
}
