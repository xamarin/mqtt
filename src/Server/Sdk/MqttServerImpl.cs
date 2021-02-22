using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mqtt.Sdk.Bindings;
using System.Net.Mqtt.Sdk.Flows;
using System.Net.Mqtt.Sdk.Packets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ServerProperties = System.Net.Mqtt.Server.Properties;

namespace System.Net.Mqtt.Sdk
{
	internal class MqttServerImpl : IMqttServer
	{
        static readonly ITracer tracer = Tracer.Get<MqttServerImpl>();

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

        internal MqttServerImpl (IMqttChannelListener binaryChannelListener,
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
            this.connectionProvider = new NotifyingConnectionProvider(this, connectionProvider);
            this.undeliveredMessagesListener = undeliveredMessagesListener;
            this.configuration = configuration;
        }

        public event EventHandler<MqttUndeliveredMessage> MessageUndelivered = (sender, args) => { };

		public event EventHandler<MqttEndpointDisconnected> Stopped = (sender, args) => { };

		public event EventHandler<string> ClientConnected;

		public event EventHandler<string> ClientDisconnected;

		public int ActiveConnections { get { return channels.Where (c => c.IsConnected).Count (); } }

		public IEnumerable<string> ActiveClients { get { return connectionProvider.ActiveClients; } }

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
                .CreateClientAsync (configuration)
                .ConfigureAwait (continueOnCapturedContext: false);
            var clientId = GetPrivateClientId ();

            await client
                .ConnectAsync (new MqttClientCredentials (clientId))
                .ConfigureAwait (continueOnCapturedContext: false);

            connectionProvider.RegisterPrivateClient (clientId);

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
                        channel.CloseAsync ().FireAndForget ();
                    }

                    channels.Clear ();

                    channelSubscription?.Dispose ();

                    foreach (var binaryChannelProvider in binaryChannelListeners) {
                        binaryChannelProvider?.Dispose ();
                    }

                    Stopped (this, new MqttEndpointDisconnected (DisconnectedReason.SelfDisconnected));
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
						packetChannel.CloseAsync ().FireAndForget ();
				        packetListener.Dispose ();
			        }, () => {
				        tracer.Warn (ServerProperties.Resources.Server_PacketsObservableCompleted);
						packetChannel.CloseAsync ().FireAndForget ();
				        packetListener.Dispose ();
			        }
                );

			channels.Add (packetChannel);
		}

        string GetPrivateClientId ()
        {
			var clientId = MqttClient.GetPrivateClientId ();

            if (connectionProvider.PrivateClients.Contains (clientId)) {
                return GetPrivateClientId ();
            }

            return clientId;
        }

		void RaiseClientConnected(string clientId)
		{
			ClientConnected?.Invoke(this, clientId);
		}

		void RaiseClientDisconnected(string clientId)
		{
			ClientDisconnected?.Invoke(this, clientId);
		}


		class NotifyingConnectionProvider : IConnectionProvider
		{
			IConnectionProvider connections;
			MqttServerImpl server;


			public NotifyingConnectionProvider(MqttServerImpl server, IConnectionProvider connections)
			{
				this.server = server;
				this.connections = connections;
			}

			public async Task AddConnectionAsync(string clientId, IMqttChannel<IPacket> connection)
			{
				await connections.AddConnectionAsync(clientId, connection).ConfigureAwait(continueOnCapturedContext: false);
				server.RaiseClientConnected(clientId);
			}

			public async Task RemoveConnectionAsync(string clientId)
			{
				await connections.RemoveConnectionAsync(clientId).ConfigureAwait(continueOnCapturedContext: false);
				server.RaiseClientDisconnected(clientId);
			}

			public IEnumerable<string> ActiveClients => connections.ActiveClients;

			public int Connections => connections.Connections;

			public IEnumerable<string> PrivateClients => connections.PrivateClients;

			public Task<IMqttChannel<IPacket>> GetConnectionAsync(string clientId) => connections.GetConnectionAsync(clientId);

			public void RegisterPrivateClient(string clientId) => connections.RegisterPrivateClient(clientId);
		}
    }
}
