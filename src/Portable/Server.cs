using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Hermes.Packets;

namespace Hermes
{
	public class Server : IDisposable
	{
		readonly IObservable<IBufferedChannel<byte>> listener;
		readonly IObservable<Unit> seconds;
		readonly IPacketChannelFactory factory;
		readonly IMessagingHandler handler;
		readonly IList<IBufferedChannel<byte>> sockets = new List<IBufferedChannel<byte>> ();
		readonly IList<string> activeClients = new List<string> ();

		public Server (IObservable<IBufferedChannel<byte>> listener, IObservable<Unit> seconds, IPacketChannelFactory factory, IMessagingHandler handler)
		{
			this.listener = listener;
			this.seconds = seconds;
			this.factory = factory;
			this.handler = handler;

			this.listener.Subscribe (socket => {
				this.sockets.Add (socket);

				var timeout = this.seconds.Skip (59).Take (1).Subscribe (_ => socket.Close ());
				var packet = this.factory.CreateChannel (socket);

				packet.Receiver.OfType<Connect> ().Subscribe (connect => {
					timeout.Dispose ();
					this.activeClients.Add (connect.ClientId);
					this.handler.Handle (connect.ClientId, packet);
				});

				packet.Receiver.Subscribe (_ => { },
					ex => { socket.Close (); sockets.Remove (socket); },
					() => { socket.Close (); sockets.Remove (socket); });
			});
		}

		~Server ()
		{
			Dispose (false);
		}

		public int ActiveSockets { get { return this.sockets.Count; } }

		public IEnumerable<string> ActiveClients { get { return this.activeClients; } }

		public void Close ()
		{
			this.Dispose (true);
		}

		protected void Dispose (bool disposing)
		{
			if (disposing) {
				foreach (var channel in sockets) {
					channel.Close ();
				}

				GC.SuppressFinalize (this);
			}
		}

		void IDisposable.Dispose ()
		{
			this.Dispose (true);
		}
	}
}
