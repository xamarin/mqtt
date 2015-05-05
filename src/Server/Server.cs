using System;
using System.Collections.Generic;
using System.Linq;
using Hermes.Diagnostics;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Properties;
using System.Reactive;

namespace Hermes
{
	public class Server : IDisposable
	{
		static readonly ITracer tracer = Tracer.Get<Server> ();

		bool disposed;
		IDisposable channelSubscription;
		IDisposable streamSubscription;

		readonly IObservable<IChannel<byte[]>> binaryChannelProvider;
		readonly IPacketChannelFactory channelFactory;
		readonly IProtocolFlowProvider flowProvider;
		readonly IConnectionProvider connectionProvider;
		readonly IEventStream eventStream;
		readonly ProtocolConfiguration configuration;

		readonly IList<IChannel<IPacket>> channels = new List<IChannel<IPacket>> ();

		public Server (IObservable<IChannel<byte[]>> binaryChannelProvider, 
			IPacketChannelFactory channelFactory,
			IProtocolFlowProvider flowProvider,
			IConnectionProvider connectionProvider,
			IEventStream eventStream,
			ProtocolConfiguration configuration)
		{
			this.binaryChannelProvider = binaryChannelProvider;
			this.channelFactory = channelFactory;
			this.flowProvider = flowProvider;
			this.connectionProvider = connectionProvider;
			this.eventStream = eventStream;
			this.configuration = configuration;
		}

		public event EventHandler<TopicNotSubscribed> TopicNotSubscribed = (sender, args) => { };

		public event EventHandler<ClosedEventArgs> Stopped = (sender, args) => { };

		public int ActiveChannels { get { return this.channels.Where(c => c.IsConnected).Count(); } }

		public IEnumerable<string> ActiveClients { get { return this.connectionProvider.ActiveClients; } }

		/// <exception cref="ProtocolException">ProtocolException</exception>
		/// <exception cref="ObjectDisposedException">ObjectDisposedException</exception>
		public void Start()
		{
			if (this.disposed)
				throw new ObjectDisposedException (this.GetType ().FullName);

			this.channelSubscription = this.binaryChannelProvider
				.Subscribe (
					binaryChannel => this.ProcessChannel(binaryChannel), 
					ex => { tracer.Error (ex); }, 
					() => {}	
				);

			this.streamSubscription = this.eventStream
				.Of<TopicNotSubscribed> ()
				.Subscribe (e => {
					this.TopicNotSubscribed (this, e);
				});
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

				if (this.streamSubscription != null) {
					this.streamSubscription.Dispose ();
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
			tracer.Info (Resources.Tracer_Server_NewSocketAccepted);

			var packetChannel = this.channelFactory.Create (binaryChannel);
			var packetListener = new ServerPacketListener (this.connectionProvider, this.flowProvider, this.configuration);

			packetListener.Listen (packetChannel);
			packetListener.Packets.Subscribe (_ => {}, ex => { 
				tracer.Error (ex);
				this.CloseChannel (packetChannel);
			}, () => {
				tracer.Warn (Resources.Tracer_Server_PacketsObservableCompleted);

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
