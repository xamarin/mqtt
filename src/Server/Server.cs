using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Hermes.Diagnostics;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes
{
	public class Server : IDisposable
	{
		static readonly ITracer tracer = Tracer.Get<Server> ();

		readonly IObservable<IBufferedChannel<byte>> socketProvider;
		readonly ProtocolConfiguration configuration;
		readonly IPacketChannelFactory channelFactory;
		readonly IPacketChannelAdapter channelAdapter;

		readonly IList<IBufferedChannel<byte>> sockets = new List<IBufferedChannel<byte>> ();
		readonly IList<string> activeClients = new List<string> ();

		public Server (IObservable<ReactiveSocketChannel> socketProvider, IRepositoryFactory repositoryFactory, 
			ProtocolConfiguration configuration)
			: this(socketProvider, new PacketChannelFactory(new TopicEvaluator(configuration)), 
				new PacketChannelAdapter(repositoryFactory, configuration), configuration)
		{
		}

		public Server (
			IObservable<IBufferedChannel<byte>> socketProvider, 
			IPacketChannelFactory channelFactory, 
			IPacketChannelAdapter channelAdapter,
			ProtocolConfiguration configuration)
		{
			this.socketProvider = socketProvider;
			this.channelFactory = channelFactory;
			this.channelAdapter = channelAdapter;
			this.configuration = configuration;

			this.socketProvider.Subscribe (
				socket => this.ProcessSocket(socket), 
				ex => { tracer.Error (ex.Message); }, 
				() => {}	
			);
		}

		public int ActiveSockets { get { return this.sockets.Count; } }

		public IEnumerable<string> ActiveClients { get { return this.activeClients; } }

		public void Close ()
		{
			this.Dispose (true);
		}

		public void Dispose ()
		{
			this.Dispose (true);
		}

		void IDisposable.Dispose ()
		{
			this.Dispose (true);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing) {
				foreach (var channel in sockets) {
					channel.Close ();
				}

				GC.SuppressFinalize (this);
			}
		}

		private void ProcessSocket(IBufferedChannel<byte> socket)
		{
			this.sockets.Add (socket);
			
			var clientId = string.Empty;

			var packetChannel = this.channelFactory.CreateChannel (socket);
			var protocolChannel = this.channelAdapter.Adapt (packetChannel);

			protocolChannel.Sender.Subscribe (packet => {
				if(packet is ConnectAck)
					this.activeClients.Add (clientId);
			}, ex => {
				tracer.Error (ex.Message);
				this.CloseSocket (socket);
			}, () => {
				this.CloseSocket (socket);	
			});

			protocolChannel.Receiver.OfType<Connect> ().Subscribe (connect => {
				clientId = connect.ClientId;
			});

			protocolChannel.Receiver.Subscribe (_ => {}, ex => { 
				tracer.Error (ex.Message);
				this.CloseSocket (socket);
			}, () => { 
				this.CloseSocket (socket);
			});
		}

		private void CloseSocket(IBufferedChannel<byte> socket)
		{
			this.sockets.Remove (socket);
			socket.Close ();
		}
	}
}
