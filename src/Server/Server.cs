using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Hermes.Packets;

namespace Hermes
{
	//TODO: Add Tracing compatible with PCL
	public class Server : IDisposable
	{
		readonly IObservable<IBufferedChannel<byte>> socketListener;
		readonly IObservable<Unit> timeListener;
		readonly IProtocolConfiguration configuration;
		readonly IPacketChannelFactory factory;
		readonly IMessagingHandler handler;
		readonly IList<IBufferedChannel<byte>> sockets = new List<IBufferedChannel<byte>> ();
		readonly IList<string> activeClients = new List<string> ();

		public Server (
			IObservable<IBufferedChannel<byte>> socketListener, 
			IObservable<Unit> timeListener, 
			IProtocolConfiguration configuration, 
			IPacketChannelFactory factory, 
			IMessagingHandler handler)
		{
			this.socketListener = socketListener;
			this.timeListener = timeListener;
			this.configuration = configuration;
			this.factory = factory;
			this.handler = handler;

			this.socketListener.Subscribe (socket => {
				this.sockets.Add (socket);

				var timeout = this.timeListener.Skip (this.configuration.ConnectTimeWindow).Take (1).Subscribe (_ => {
					//tracer.Error (Resources.Server_NoConnectReceived);
					socket.Close ();
				});

				var isConnected = false;
				var packet = this.factory.CreateChannel (socket);

				this.handler.Handle (packet);

				var keepAlive = 0;
				var keepAliveTimeout = default (IDisposable);

				packet.Receiver.OfType<Connect> ().Subscribe (connect => {
					if (isConnected) {
						//tracer.Error (Resources.Server_SecondConnectNotAllowed);
						socket.Close (); 
						sockets.Remove (socket);
						return;
					}
						 
					timeout.Dispose ();
					isConnected = true;
					keepAlive = connect.KeepAlive;
					this.activeClients.Add (connect.ClientId);
				});

				packet.Receiver.Subscribe (p => {
					if (!isConnected && !(p is Connect)) {
						//tracer.Error (Resources.Server_FirstPacketMustBeConnect);
						socket.Close (); 
						sockets.Remove (socket);
					}

					//TODO: Need to analyze keep alive monitoring also for delivered messages to clients
					if (keepAlive > 0) {
						if (keepAliveTimeout != null) {
							keepAliveTimeout.Dispose ();
						}

						keepAliveTimeout = this.timeListener.Skip ((int)(keepAlive* 1.5) - 1).Take (1).Subscribe (_ => {
							socket.Close ();
						});
					}
				}, ex => { socket.Close (); sockets.Remove (socket); }, 
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
