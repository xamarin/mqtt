﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
using System.Reactive;

namespace System.Net.Mqtt
{
	public class Server : IDisposable
	{
		static readonly ITracer tracer = Tracer.Get<Server> ();

		bool disposed;
		IDisposable channelSubscription;
		IDisposable streamSubscription;

		readonly IChannelProvider binaryChannelProvider;
		readonly IPacketChannelFactory channelFactory;
		readonly IProtocolFlowProvider flowProvider;
		readonly IConnectionProvider connectionProvider;
		readonly IEventStream eventStream;
		readonly ProtocolConfiguration configuration;

		readonly IList<IChannel<IPacket>> channels = new List<IChannel<IPacket>> ();

		internal Server (IChannelProvider binaryChannelProvider, 
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
				.GetChannels()
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
			this.Stop (ClosedReason.Disposed);
		}

		void IDisposable.Dispose ()
		{
			this.Stop ();
		}

		protected virtual void Dispose (bool disposing)
		{
			if (this.disposed) return;

			if (disposing) {
				tracer.Info (Properties.Resources.Tracer_Disposing, this.GetType ().FullName);

				if (this.channelSubscription != null) {
					this.channelSubscription.Dispose ();
				}

				if (this.streamSubscription != null) {
					this.streamSubscription.Dispose ();
				}

				this.binaryChannelProvider.Dispose ();

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
			tracer.Verbose (Properties.Resources.Tracer_Server_NewSocketAccepted);

			var packetChannel = this.channelFactory.Create (binaryChannel);
			var packetListener = new ServerPacketListener (packetChannel, this.connectionProvider, this.flowProvider, this.configuration);

			packetListener.Listen ();
			packetListener.Packets.Subscribe (_ => {}, ex => { 
				tracer.Error (ex, Properties.Resources.Tracer_Server_PacketsObservableError);
				packetChannel.Dispose ();
				packetListener.Dispose ();
			}, () => {
				tracer.Warn (Properties.Resources.Tracer_Server_PacketsObservableCompleted);
				packetChannel.Dispose ();
				packetListener.Dispose ();
			});

			this.channels.Add (packetChannel);
		}
	}
}
