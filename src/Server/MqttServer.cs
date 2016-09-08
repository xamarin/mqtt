using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mqtt.Bindings;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ServerProperties = System.Net.Mqtt.Server.Properties;

namespace System.Net.Mqtt
{
    internal class MqttServer : IMqttServer
	{
        static readonly ITracer tracer = Tracer.Get<MqttServer>();

        bool started;
        bool disposed;
		IDisposable channelSubscription;
		IDisposable streamSubscription;

        readonly IEnumerable<IMqttChannelListener> binaryChannelListeners;
		readonly IPacketChannelFactory channelFactory;
		readonly IProtocolFlowProvider flowProvider;
		readonly IConnectionProvider connectionProvider;
        readonly ISubject<MqttUndeliveredMessage> undeliveredMessagesListener;
        readonly MqttConfiguration configuration;
        readonly ISubject<PrivateStream> privateStreamListener;
        readonly IList<IMqttChannel<IPacket>> channels = new List<IMqttChannel<IPacket>>();

        internal MqttServer (IMqttChannelListener binaryChannelListener,
            IPacketChannelFactory channelFactory,
            IProtocolFlowProvider flowProvider,
            IConnectionProvider connectionProvider,
            ISubject<MqttUndeliveredMessage> undeliveredMessagesListener,
            MqttConfiguration configuration)
		{
            privateStreamListener = new Subject<PrivateStream> ();
            binaryChannelListeners = new[] { new PrivateChannelListener (privateStreamListener, configuration), binaryChannelListener };
            this.channelFactory = channelFactory;
            this.flowProvider = flowProvider;
            this.connectionProvider = connectionProvider;
            this.undeliveredMessagesListener = undeliveredMessagesListener;
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
                throw new ObjectDisposedException (nameof (Server));

            var channelStreams = binaryChannelListeners.Select (listener => listener.GetChannelStream ());

            channelSubscription = Observable
                .Merge (channelStreams)
                .Subscribe (
                    binaryChannel => ProcessChannel (binaryChannel),
                    ex => { tracer.Error (ex); },
                    () => { }
                );

			streamSubscription = undeliveredMessagesListener
                .Subscribe (e => {
				    MessageUndelivered (this, e);
			    });

            started = true;
        }

        public async Task<IMqttConnectedClient> CreateClientAsync ()
        {
            if (disposed)
                throw new ObjectDisposedException (nameof (Server));

            if (!started)
                throw new InvalidOperationException (ServerProperties.Resources.Server_NotStartedError);

            var factory = new MqttConnectedClientFactory (privateStreamListener);
            var client = await factory
                .CreateAsync (configuration)
                .ConfigureAwait (continueOnCapturedContext: false);
            var clientId = GetPrivateClientId ();

            await client
                .ConnectAsync (new MqttClientCredentials (clientId))
                .ConfigureAwait (continueOnCapturedContext: false);

            return client;
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
                    tracer.Info (Properties.Resources.Mqtt_Disposing, GetType ().FullName);

                    streamSubscription?.Dispose ();

                    foreach (var channel in channels)
                    {
                        channel.Dispose ();
                    }

                    channels.Clear ();

                    channelSubscription?.Dispose ();

                    foreach (var binaryChannelProvider in binaryChannelListeners) {
                        binaryChannelProvider?.Dispose();
                    }

                    Stopped (this, new MqttEndpointDisconnected (DisconnectedReason.Disposed));
                } catch (Exception ex) {
                    tracer.Error (ex);
                    Stopped (this, new MqttEndpointDisconnected (DisconnectedReason.Error, ex.Message));
                } finally {
                    started = false;
                    disposed = true;
                }
			}
		}

		void ProcessChannel (IMqttChannel<byte[]> binaryChannel)
		{
			tracer.Verbose (ServerProperties.Resources.Server_NewSocketAccepted);

			var packetChannel = channelFactory.Create (binaryChannel);
			var packetListener = new ServerPacketListener (packetChannel, connectionProvider, flowProvider, configuration);

			packetListener.Listen ();
			packetListener
                .PacketStream
                .Subscribe (_ => { }, ex => {
				        tracer.Error (ex, ServerProperties.Resources.Server_PacketsObservableError);
				        packetChannel.Dispose ();
				        packetListener.Dispose ();
			        }, () => {
				        tracer.Warn (ServerProperties.Resources.Server_PacketsObservableCompleted);
				        packetChannel.Dispose ();
				        packetListener.Dispose ();
			        }
                );

			channels.Add (packetChannel);
		}

        string GetPrivateClientId () => $"private{Guid.NewGuid ().ToString ().Split ('-').First ()}";
    }
}
