using Merq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Server
{
    internal class Server : IMqttServer
	{
        static readonly ITracer tracer = Tracer.Get<Server>();

        bool disposed;
		IDisposable channelSubscription;
		IDisposable streamSubscription;

		readonly IMqttChannelProvider binaryChannelProvider;
		readonly IPacketChannelFactory channelFactory;
		readonly IProtocolFlowProvider flowProvider;
		readonly IConnectionProvider connectionProvider;
		readonly IEventStream eventStream;
		readonly MqttConfiguration configuration;

		readonly IList<IMqttChannel<IPacket>> channels = new List<IMqttChannel<IPacket>> ();

		internal Server (IMqttChannelProvider binaryChannelProvider,
			IPacketChannelFactory channelFactory,
			IProtocolFlowProvider flowProvider,
			IConnectionProvider connectionProvider,
			IEventStream eventStream,
			MqttConfiguration configuration)
		{
			this.binaryChannelProvider = binaryChannelProvider;
			this.channelFactory = channelFactory;
			this.flowProvider = flowProvider;
			this.connectionProvider = connectionProvider;
			this.eventStream = eventStream;
			this.configuration = configuration;
		}

		public event EventHandler<MqttUndeliveredMessage> MessageUndelivered = (sender, args) => { };

		public event EventHandler<MqttEndpointDisconnected> Stopped = (sender, args) => { };

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
            Dispose (disposing: true);
            GC.SuppressFinalize (this);
        }

		void IDisposable.Dispose ()
		{
			Stop ();
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed) return;

			if (disposing) {
                try
                {
                    tracer.Info (Resources.Tracer_Disposing, GetType ().FullName);

                    streamSubscription?.Dispose ();

                    foreach (var channel in channels)
                    {
                        channel.Dispose ();
                    }

                    channels.Clear ();

                    channelSubscription?.Dispose ();
                    binaryChannelProvider?.Dispose ();

                    Stopped (this, new MqttEndpointDisconnected (DisconnectedReason.Disposed));
                } catch (Exception ex) {
                    tracer.Error (ex);
                    Stopped (this, new MqttEndpointDisconnected (DisconnectedReason.Error, ex.Message));
                } finally {
                    disposed = true;
                }
			}
		}

		void ProcessChannel (IMqttChannel<byte[]> binaryChannel)
		{
			tracer.Verbose (Resources.Tracer_Server_NewSocketAccepted);

			var packetChannel = channelFactory.Create (binaryChannel);
			var packetListener = new ServerPacketListener (packetChannel, connectionProvider, flowProvider, configuration);

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
