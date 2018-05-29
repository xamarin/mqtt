using System.Diagnostics;
using System.Linq;
using System.Net.Mqtt.Sdk.Flows;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk
{
	internal class MqttClientImpl : IMqttClient
	{
        static readonly ITracer tracer = Tracer.Get<MqttClientImpl> ();

		bool disposed;
		bool isProtocolConnected;
		IPacketListener packetListener;
		IDisposable packetsSubscription;

		readonly ReplaySubject<MqttApplicationMessage> receiver;
		readonly ReplaySubject<IPacket> sender;
		readonly IPacketChannelFactory channelFactory;
		readonly IProtocolFlowProvider flowProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IPacketIdProvider packetIdProvider;
		readonly MqttConfiguration configuration;
		readonly TaskRunner clientSender;

		internal MqttClientImpl (IPacketChannelFactory channelFactory,
			IProtocolFlowProvider flowProvider,
			IRepositoryProvider repositoryProvider,
			IPacketIdProvider packetIdProvider,
			MqttConfiguration configuration)
		{
			receiver = new ReplaySubject<MqttApplicationMessage> (window: TimeSpan.FromSeconds (configuration.WaitTimeoutSecs));
			sender = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds (configuration.WaitTimeoutSecs));

			this.channelFactory = channelFactory;
			this.flowProvider = flowProvider;
			sessionRepository = repositoryProvider.GetRepository<ClientSession> ();
			this.packetIdProvider = packetIdProvider;
			this.configuration = configuration;
			clientSender = TaskRunner.Get ();
        }

		public event EventHandler<MqttEndpointDisconnected> Disconnected = (sender, args) => { };

		public string Id { get; private set; }

		public bool IsConnected
		{
			get
			{
				CheckUnderlyingConnection ();

				return isProtocolConnected && Channel.IsConnected;
			}
			private set
			{
				isProtocolConnected = value;
			}
		}

		public IObservable<MqttApplicationMessage> MessageStream { get { return receiver; } }

        internal IMqttChannel<IPacket> Channel { get; private set; }

        public async Task ConnectAsync (MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			try {
				if (IsConnected) {
					throw new MqttClientException (string.Format (Properties.Resources.Client_AlreadyConnected, Id));
				}

				Id = credentials.ClientId;
				OpenClientSession (credentials.ClientId, cleanSession);

				await InitializeChannelAsync ().ConfigureAwait (continueOnCapturedContext: false);

				var connect = new Connect (credentials.ClientId, cleanSession) {
					UserName = credentials.UserName,
					Password = credentials.Password,
					Will = will,
					KeepAlive = configuration.KeepAliveSecs
				};

				await SendPacketAsync (connect)
					.ConfigureAwait (continueOnCapturedContext: false);

				var connectTimeout = TimeSpan.FromSeconds (configuration.WaitTimeoutSecs);
				var ack = await packetListener
                    .PacketStream
					.ObserveOn (NewThreadScheduler.Default)
					.OfType<ConnectAck> ()
					.FirstOrDefaultAsync ()
					.Timeout (connectTimeout);

				if (ack == null) {
					var message = string.Format(Properties.Resources.Client_ConnectionDisconnected, credentials.ClientId);

					throw new MqttClientException (message);
				}

				if (ack.Status != MqttConnectionStatus.Accepted) {
					throw new MqttConnectionException (ack.Status);
				}

				IsConnected = true;
			} catch (TimeoutException timeEx) {
				Close (timeEx);
				throw new MqttClientException (string.Format (Properties.Resources.Client_ConnectionTimeout, credentials.ClientId), timeEx);
			} catch (MqttConnectionException connectionEx) {
				Close (connectionEx);

				var message = string.Format (Properties.Resources.Client_ConnectNotAccepted, credentials.ClientId, connectionEx.ReturnCode);

				throw new MqttClientException (message, connectionEx);
			} catch (MqttClientException clientEx) {
				Close (clientEx);
				throw;
			} catch (Exception ex) {
				Close (ex);
				throw new MqttClientException (string.Format (Properties.Resources.Client_ConnectionError, credentials.ClientId), ex);
			}
		}

		public async Task SubscribeAsync (string topicFilter, MqttQualityOfService qos)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			try {
				var packetId = packetIdProvider.GetPacketId ();
				var subscribe = new Subscribe (packetId, new Subscription (topicFilter, qos));

				var ack = default (SubscribeAck);
				var subscribeTimeout = TimeSpan.FromSeconds (configuration.WaitTimeoutSecs);

				await SendPacketAsync (subscribe)
					.ConfigureAwait (continueOnCapturedContext: false);

				ack = await packetListener
                    .PacketStream
					.ObserveOn (NewThreadScheduler.Default)
					.OfType<SubscribeAck> ()
					.FirstOrDefaultAsync (x => x.PacketId == packetId)
					.Timeout (subscribeTimeout);

				if (ack == null) {
					var message = string.Format(Properties.Resources.Client_SubscriptionDisconnected, Id, topicFilter);

					tracer.Error (message);

					throw new MqttClientException (message);
				}

                if (ack.ReturnCodes.FirstOrDefault () == SubscribeReturnCode.Failure) {
                    var message = string.Format(Properties.Resources.Client_SubscriptionRejected, Id, topicFilter);

                    tracer.Error(message);

                    throw new MqttClientException (message);
                }
			} catch (TimeoutException timeEx) {
				Close (timeEx);

				var message = string.Format (Properties.Resources.Client_SubscribeTimeout, Id, topicFilter);

				throw new MqttClientException (message, timeEx);
			} catch (MqttClientException clientEx) {
				Close (clientEx);
				throw;
			} catch (Exception ex) {
				Close (ex);

				var message = string.Format (Properties.Resources.Client_SubscribeError, Id, topicFilter);

				throw new MqttClientException (message, ex);
			}
		}

		public async Task PublishAsync (MqttApplicationMessage message, MqttQualityOfService qos, bool retain = false)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			try {
				ushort? packetId = qos == MqttQualityOfService.AtMostOnce ? null : (ushort?)packetIdProvider.GetPacketId ();
				var publish = new Publish (message.Topic, qos, retain, duplicated: false, packetId: packetId)
				{
					Payload = message.Payload
				};

				var senderFlow = flowProvider.GetFlow<PublishSenderFlow> ();

				await clientSender.Run (async () => {
					await senderFlow.SendPublishAsync (Id, publish, Channel)
						.ConfigureAwait (continueOnCapturedContext: false);
				}).ConfigureAwait (continueOnCapturedContext: false);
			} catch (Exception ex) {
				Close (ex);
				throw;
			}
		}

		public async Task UnsubscribeAsync (params string[] topics)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			try {
                topics = topics ?? new string[] { };

				var packetId = packetIdProvider.GetPacketId ();
				var unsubscribe = new Unsubscribe(packetId, topics);

				var ack = default (UnsubscribeAck);
				var unsubscribeTimeout = TimeSpan.FromSeconds(configuration.WaitTimeoutSecs);

				await SendPacketAsync (unsubscribe)
					.ConfigureAwait (continueOnCapturedContext: false);

				ack = await packetListener
                    .PacketStream
					.ObserveOn (NewThreadScheduler.Default)
					.OfType<UnsubscribeAck> ()
					.FirstOrDefaultAsync (x => x.PacketId == packetId)
					.Timeout (unsubscribeTimeout);

				if (ack == null) {
					var message = string.Format(Properties.Resources.Client_UnsubscribeDisconnected, Id, string.Join(", ", topics));

					tracer.Error (message);

					throw new MqttClientException (message);
				}
			} catch (TimeoutException timeEx) {
				Close (timeEx);

				var message = string.Format (Properties.Resources.Client_UnsubscribeTimeout, Id, string.Join(", ", topics));

				tracer.Error (message);

				throw new MqttClientException (message, timeEx);
			} catch (MqttClientException clientEx) {
				Close (clientEx);
				throw;
			} catch (Exception ex) {
				Close (ex);

				var message = string.Format (Properties.Resources.Client_UnsubscribeError, Id, string.Join(", ", topics));

				tracer.Error (message);

				throw new MqttClientException (message, ex);
			}
		}

		public async Task DisconnectAsync ()
		{
			try {
				if (!IsConnected) {
					throw new MqttClientException (Properties.Resources.Client_AlreadyDisconnected);
				}

				packetsSubscription?.Dispose ();

				await SendPacketAsync (new Disconnect ())
					.ConfigureAwait (continueOnCapturedContext: false);

				await packetListener
					.PacketStream
					.LastOrDefaultAsync ();

				Close (DisconnectedReason.SelfDisconnected);
			} catch (Exception ex) {
				Close (ex);
			}
		}

		void IDisposable.Dispose ()
		{
            DisposeAsync (disposing: true).Wait ();
			GC.SuppressFinalize (this);
		}

		protected virtual async Task DisposeAsync (bool disposing)
		{
			if (disposed) return;

			if (disposing) {
				if (IsConnected) {
					await DisconnectAsync ().ConfigureAwait (continueOnCapturedContext: false);
				}

				sender?.OnCompleted ();
				receiver?.OnCompleted ();
				(clientSender as IDisposable)?.Dispose ();
				disposed = true;
			}
		}

		void Close (Exception ex)
		{
			tracer.Error (ex);
			Close (DisconnectedReason.Error, ex.Message);
		}

		void Close (DisconnectedReason reason, string message = null)
		{
			tracer.Info (Properties.Resources.Client_Closing, Id, reason);

			CloseClientSession ();
			packetsSubscription?.Dispose ();
			packetListener?.Dispose ();
            Channel?.Dispose ();
            IsConnected = false;
            Id = null;

            Disconnected (this, new MqttEndpointDisconnected (reason, message));
		}

		async Task InitializeChannelAsync ()
		{
			Channel = await channelFactory
				.CreateAsync ()
				.ConfigureAwait (continueOnCapturedContext: false);

			packetListener = new ClientPacketListener (Channel, flowProvider, configuration);
			packetListener.Listen ();
			ObservePackets ();
		}

		void OpenClientSession (string clientId, bool cleanSession)
		{
			var session = sessionRepository.Get (clientId);
			var sessionPresent = cleanSession ? false : session != null;

			if (cleanSession && session != null) {
				sessionRepository.Delete (session.Id);
				session = null;

				tracer.Info (Properties.Resources.Client_CleanedOldSession, clientId);
			}

			if (session == null) {
				session = new ClientSession (clientId, cleanSession);

				sessionRepository.Create (session);

				tracer.Info (Properties.Resources.Client_CreatedSession, clientId);
			}
		}

		void CloseClientSession ()
		{
			var session = string.IsNullOrEmpty (Id) ? default (ClientSession) : sessionRepository.Get (Id);

			if (session == null) {
				return;
			}

			if (session.Clean) {
				sessionRepository.Delete (session.Id);

				tracer.Info (Properties.Resources.Client_DeletedSessionOnDisconnect, Id);
			}
		}

		async Task SendPacketAsync (IPacket packet)
		{
			sender.OnNext (packet);

			await clientSender.Run (async () => await Channel.SendAsync (packet).ConfigureAwait (continueOnCapturedContext: false))
				.ConfigureAwait (continueOnCapturedContext: false);
		}

		void CheckUnderlyingConnection ()
		{
			if (isProtocolConnected && !Channel.IsConnected) {
				Close (DisconnectedReason.Error, Properties.Resources.Client_UnexpectedChannelDisconnection);
			}
		}

		void ObservePackets ()
		{
			packetsSubscription = packetListener
                .PacketStream
				.ObserveOn (NewThreadScheduler.Default)
				.Subscribe (packet => {
					if (packet.Type == MqttPacketType.Publish) {
						var publish = packet as Publish;
						var message = new MqttApplicationMessage (publish.Topic, publish.Payload);

						receiver.OnNext (message);
						tracer.Info (Properties.Resources.Client_NewApplicationMessageReceived, Id, publish.Topic);
					}
				}, ex => {
					Close (ex);
				}, () => {
					tracer.Warn (Properties.Resources.Client_PacketsObservableCompleted);
					Close (DisconnectedReason.RemoteDisconnected);
				});
		}
	}
}
