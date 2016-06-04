using Merq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
using System.Reactive;

namespace System.Net.Mqtt.Server
{
	public class Server : IDisposable
	{
		bool disposed;
		IDisposable channelSubscription;
		IDisposable streamSubscription;

		readonly ITracer tracer;
		readonly IChannelProvider binaryChannelProvider;
		readonly IPacketChannelFactory channelFactory;
		readonly IProtocolFlowProvider flowProvider;
		readonly IConnectionProvider connectionProvider;
		readonly IEventStream eventStream;
		readonly ITracerManager tracerManager;
		readonly ProtocolConfiguration configuration;

		readonly IList<IChannel<IPacket>> channels = new List<IChannel<IPacket>> ();

		internal Server (IChannelProvider binaryChannelProvider,
			IPacketChannelFactory channelFactory,
			IProtocolFlowProvider flowProvider,
			IConnectionProvider connectionProvider,
			IEventStream eventStream,
			ITracerManager tracerManager,
			ProtocolConfiguration configuration)
		{
			tracer = tracerManager.Get<Server> ();
			this.binaryChannelProvider = binaryChannelProvider;
			this.channelFactory = channelFactory;
			this.flowProvider = flowProvider;
			this.connectionProvider = connectionProvider;
			this.eventStream = eventStream;
			this.tracerManager = tracerManager;
			this.configuration = configuration;
		}

		public event EventHandler<TopicNotSubscribed> TopicNotSubscribed = (sender, args) => { };

		public event EventHandler<ClosedEventArgs> Stopped = (sender, args) => { };

		public int ActiveChannels { get { return channels.Where (c => c.IsConnected).Count (); } }

		public IEnumerable<string> ActiveClients { get { return connectionProvider.ActiveClients; } }

		/// <exception cref="ProtocolException">ProtocolException</exception>
		/// <exception cref="ObjectDisposedException">ObjectDisposedException</exception>
		public void Start ()
		{
			if (disposed)
				throw new ObjectDisposedException (GetType ().FullName);

			channelSubscription = binaryChannelProvider
				.GetChannels ()
				.Subscribe (
					binaryChannel => ProcessChannel (binaryChannel),
					ex => { tracer.Error (ex); },
					() => { }
				);

			streamSubscription = eventStream
				.Of<TopicNotSubscribed> ()
				.Subscribe (e => {
					TopicNotSubscribed (this, e);
				});
		}

		public void Stop ()
		{
			Stop (ClosedReason.Disposed);
		}

		void IDisposable.Dispose ()
		{
			Stop ();
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed) return;

			if (disposing) {
				tracer.Info (Properties.Resources.Tracer_Disposing, GetType ().FullName);

				if (streamSubscription != null) {
					streamSubscription.Dispose ();
				}

				foreach (var channel in channels) {
					channel.Dispose ();
				}

				channels.Clear ();

				if (channelSubscription != null) {
					channelSubscription.Dispose ();
				}

				binaryChannelProvider.Dispose ();

				disposed = true;
			}
		}

		void Stop (ClosedReason reason, string message = null)
		{
			Dispose (true);
			Stopped (this, new ClosedEventArgs (reason, message));
			GC.SuppressFinalize (this);
		}

		void ProcessChannel (IChannel<byte[]> binaryChannel)
		{
			tracer.Verbose (Properties.Resources.Tracer_Server_NewSocketAccepted);

			var packetChannel = channelFactory.Create (binaryChannel);
			var packetListener = new ServerPacketListener (packetChannel, connectionProvider, flowProvider, tracerManager, configuration);

			packetListener.Listen ();
			packetListener.Packets.Subscribe (_ => { }, ex => {
				tracer.Error (ex, Properties.Resources.Tracer_Server_PacketsObservableError);
				packetChannel.Dispose ();
				packetListener.Dispose ();
			}, () => {
				tracer.Warn (Properties.Resources.Tracer_Server_PacketsObservableCompleted);
				packetChannel.Dispose ();
				packetListener.Dispose ();
			});

			channels.Add (packetChannel);
		}
	}
}
