using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Hermes.Diagnostics;
using Hermes.Flows;
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
				ex => { tracer.Error (ex); }, 
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

			protocolChannel.Sender.Subscribe (_ => {
			}, ex => {
				tracer.Error (ex);
				this.CloseSocket (socket, clientId);
			}, () => {
				this.CloseSocket (socket, clientId);	
			});

			protocolChannel.Receiver.Subscribe (packet => {
				var connect = packet as Connect;
				if (connect != null)
					this.activeClients.Add (connect.ClientId);
			}, ex => { 
				tracer.Error (ex);
				this.CloseSocket (socket, clientId);
			}, () => { 
				this.CloseSocket (socket, clientId);
			});
		}

		private void CloseSocket(IBufferedChannel<byte> socket, string clientId)
		{
			this.RemoveClientId (clientId);

			this.sockets.Remove (socket);
			socket.Close ();
		}

		private void RemoveClientId (string clientId)
		{
			if (!this.activeClients.Any (c => c == clientId))
				return;

			this.activeClients.Remove (clientId);
		}
	}
}
