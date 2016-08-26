using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Server
{
    internal class Server : IMqttServer
	{
		bool disposed;
		IDisposable channelSubscription;
		IDisposable streamSubscription;

		readonly ITracer tracer;
		readonly IMqttChannelProvider binaryChannelProvider;
		readonly IPacketChannelFactory channelFactory;
		readonly IProtocolFlowProvider flowProvider;
		readonly IConnectionProvider connectionProvider;
		readonly IEventStream eventStream;
		readonly ITracerManager tracerManager;
		readonly MqttConfiguration configuration;

		readonly IList<IMqttChannel<IPacket>> channels = new List<IMqttChannel<IPacket>> ();

		internal Server (IMqttChannelProvider binaryChannelProvider,
			IPacketChannelFactory channelFactory,
			IProtocolFlowProvider flowProvider,
			IConnectionProvider connectionProvider,
			IEventStream eventStream,
			ITracerManager tracerManager,
			MqttConfiguration configuration)
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

		public event EventHandler<MqttUndeliveredMessage> MessageUndelivered = (sender, args) => { };

		public event EventHandler<MqttServerStopped> Stopped = (sender, args) => { };

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
				.Of<MqttUndeliveredMessage> ()
				.Subscribe (e => {
					MessageUndelivered (this, e);
				});
		}

		public void Stop ()
		{
			Stop (StoppedReason.Disposed);
		}

		void IDisposable.Dispose ()
		{
			Stop ();
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed) return;

			if (disposing) {
				tracer.Info (Resources.Tracer_Disposing, GetType ().FullName);

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

		void Stop (StoppedReason reason, string message = null)
		{
			Dispose (true);
			Stopped (this, new MqttServerStopped (reason, message));
			GC.SuppressFinalize (this);
		}

		void ProcessChannel (IMqttChannel<byte[]> binaryChannel)
		{
			tracer.Verbose (Resources.Tracer_Server_NewSocketAccepted);

			var packetChannel = channelFactory.Create (binaryChannel);
			var packetListener = new ServerPacketListener (packetChannel, connectionProvider, flowProvider, tracerManager, configuration);

			packetListener.Listen ();
			packetListener.Packets.Subscribe (_ => { }, ex => {
				tracer.Error (ex, Resources.Tracer_Server_PacketsObservableError);
				packetChannel.Dispose ();
				packetListener.Dispose ();
			}, () => {
				tracer.Warn (Resources.Tracer_Server_PacketsObservableCompleted);
				packetChannel.Dispose ();
				packetListener.Dispose ();
			});

			channels.Add (packetChannel);
		}
	}
}
