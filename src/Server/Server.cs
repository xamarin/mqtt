using System;
using System.Collections.Generic;
using System.Linq;
using Hermes.Diagnostics;
using Hermes.Flows;
using Hermes.Packets;

namespace Hermes
{
	public class Server : IDisposable
	{
		static readonly ITracer tracer = Tracer.Get<Server> ();

		bool disposed;
		IDisposable channelSubscription;

		readonly IObservable<IChannel<byte[]>> binaryChannelProvider;
		readonly IPacketChannelFactory channelFactory;
		readonly IProtocolFlowProvider flowProvider;
		readonly IConnectionProvider connectionProvider;
		readonly ProtocolConfiguration configuration;

		readonly IList<IChannel<IPacket>> channels = new List<IChannel<IPacket>> ();

		public Server (IObservable<IChannel<byte[]>> binaryChannelProvider, 
			IPacketChannelFactory channelFactory,
			IProtocolFlowProvider flowProvider,
			IConnectionProvider connectionProvider,
			ProtocolConfiguration configuration)
		{
			this.binaryChannelProvider = binaryChannelProvider;
			this.channelFactory = channelFactory;
			this.flowProvider = flowProvider;
			this.connectionProvider = connectionProvider;
			this.configuration = configuration;
		}

		public event EventHandler<ClosedEventArgs> Stopped = (sender, args) => { };

		public int ActiveChannels { get { return this.channels.Where(c => c.IsConnected).Count(); } }

		public IEnumerable<string> ActiveClients { get { return this.connectionProvider.ActiveClients; } }

		public void Start()
		{
			if (this.disposed)
				throw new ObjectDisposedException (this.GetType ().FullName);

			this.channelSubscription = this.binaryChannelProvider.Subscribe (
				binaryChannel => this.ProcessChannel(binaryChannel), 
				ex => { tracer.Error (ex); }, 
				() => {}	
			);
		}

		public void Stop ()
		{
			this.Stop (ClosedReason.Disconnect);
		}

		void IDisposable.Dispose ()
		{
			this.Stop (ClosedReason.Dispose);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (this.disposed) return;

			if (disposing) {
				if (this.channelSubscription != null) {
					this.channelSubscription.Dispose ();
				}
				
				foreach (var channel in channels) {
					channel.Dispose ();
				}

				channels.Clear ();

				this.disposed = true;
			}
		}

		private void Stop (ClosedReason reason, string message = null)
		{
			this.Dispose (true);
			this.Stopped (this, new ClosedEventArgs(reason, message));
			GC.SuppressFinalize (this);
		}

		private void ProcessChannel(IChannel<byte[]> binaryChannel)
		{
			var packetChannel = this.channelFactory.Create (binaryChannel);
			var packetListener = new ServerPacketListener (this.connectionProvider, this.flowProvider, this.configuration);

			packetListener.Listen (packetChannel);
			packetListener.Packets.Subscribe (_ => {}, ex => { 
				tracer.Error (ex);
				this.CloseChannel (packetChannel);
			}, () => {
				this.CloseChannel (packetChannel);
			});

			this.channels.Add (packetChannel);
		}

		private void CloseChannel(IChannel<IPacket> channel)
		{
			channel.Dispose ();
		}
	}
}
