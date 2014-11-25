using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Hermes.Diagnostics;
using Hermes.Packets;

namespace Hermes
{
	public class Server : IDisposable
	{
		static readonly ITracer tracer = Tracer.Get<Server> ();

		readonly IObservable<IBufferedChannel<byte>> socketProvider;
		readonly ProtocolConfiguration configuration;
		readonly IPacketChannelFactory channelFactory;
		readonly ICommunicationHandler communicationHandler;

		readonly IList<IBufferedChannel<byte>> sockets = new List<IBufferedChannel<byte>> ();
		readonly IList<string> activeClients = new List<string> ();

		public Server (
			IObservable<IBufferedChannel<byte>> socketProvider, 
			IPacketChannelFactory channelFactory, 
			ICommunicationHandler communicationHandler,
			ProtocolConfiguration configuration)
		{
			this.socketProvider = socketProvider;
			this.channelFactory = channelFactory;
			this.communicationHandler = communicationHandler;
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

			var channel = this.channelFactory.CreateChannel (socket);
			var context = this.communicationHandler.Handle (channel);

			context.PendingDeliveries.Subscribe (async packet => {
				if(packet is ConnectAck)
					this.activeClients.Add (clientId);

				await channel.SendAsync (packet);
			}, ex => {
				tracer.Error (ex.Message);
				this.CloseSocket (socket);
			}, () => {
				this.CloseSocket (socket);	
			});

			channel.Receiver.OfType<Connect> ().Subscribe (connect => {
				clientId = connect.ClientId;
			});

			channel.Receiver.Subscribe (_ => {}, ex => { 
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
