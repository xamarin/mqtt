using System.Diagnostics;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    internal class MqttClient : IMqttClient
	{
        static readonly ITracer tracer = Tracer.Get<MqttClient> ();

		bool disposed;
		bool isConnected;
		IDisposable packetsSubscription;

		readonly ReplaySubject<MqttApplicationMessage> receiver;
		readonly ReplaySubject<IPacket> sender;
		readonly IMqttChannel<IPacket> packetChannel;
		readonly IProtocolFlowProvider flowProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IPacketIdProvider packetIdProvider;
		readonly MqttConfiguration configuration;
		readonly TaskRunner clientSender;
		readonly IPacketListener packetListener;

		internal MqttClient (IMqttChannel<IPacket> packetChannel,
			IProtocolFlowProvider flowProvider,
			IRepositoryProvider repositoryProvider,
			IPacketIdProvider packetIdProvider,
			MqttConfiguration configuration)
		{
			receiver = new ReplaySubject<MqttApplicationMessage> (window: TimeSpan.FromSeconds (configuration.WaitingTimeoutSecs));
			sender = new ReplaySubject<IPacket> (window: TimeSpan.FromSeconds (configuration.WaitingTimeoutSecs));

			this.packetChannel = packetChannel;
			this.flowProvider = flowProvider;
			sessionRepository = repositoryProvider.GetRepository<ClientSession> ();
			this.packetIdProvider = packetIdProvider;
			this.configuration = configuration;
			clientSender = TaskRunner.Get ();
			packetListener = new ClientPacketListener (packetChannel, flowProvider, configuration);

			packetListener.Listen ();
		}

		public event EventHandler<MqttEndpointDisconnected> Disconnected = (sender, args) => { };

		public string Id { get; private set; }

		public bool IsConnected
		{
			get
			{
				CheckUnderlyingConnection ();

				return isConnected && packetChannel.IsConnected;
			}
			private set
			{
				isConnected = value;
			}
		}

		public IObservable<MqttApplicationMessage> ReceiverStream { get { return receiver; } }

        internal IMqttChannel<IPacket> Channel {  get { return packetChannel; } }

        /// <exception cref="MqttClientException">ClientException</exception>
        public async Task ConnectAsync (MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

            if (IsConnected && !string.IsNullOrEmpty (Id)) {
                return;
            } 

			var ack = default (ConnectAck);

			try {
				OpenClientSession (credentials.ClientId, cleanSession);

				var connect = new Connect (credentials.ClientId, cleanSession) {
					UserName = credentials.UserName,
					Password = credentials.Password,
					Will = will,
					KeepAlive = configuration.KeepAliveSecs
				};

				var connectTimeout = TimeSpan.FromSeconds (configuration.WaitingTimeoutSecs);

				await SendPacketAsync (connect)
					.ConfigureAwait (continueOnCapturedContext: false);

				ack = await packetListener
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

				Id = credentials.ClientId;
				IsConnected = true;
				ObservePackets ();
			} catch (TimeoutException timeEx) {
				Close (timeEx);
				throw new MqttClientException (string.Format (Properties.Resources.Client_ConnectionTimeout, credentials.ClientId), timeEx);
			} catch (MqttConnectionException connectionEx) {
				Close (connectionEx);

				var message = string.Format(Properties.Resources.Client_ConnectNotAccepted, credentials.ClientId, connectionEx.ReturnCode);

				throw new MqttClientException (message, connectionEx);
			} catch (MqttClientException clientEx) {
				Close (clientEx);
				throw;
			} catch (Exception ex) {
				Close (ex);
				throw new MqttClientException (string.Format (Properties.Resources.Client_ConnectionError, credentials.ClientId), ex);
			}
		}

		/// <exception cref="MqttClientException">ClientException</exception>
		public async Task SubscribeAsync (string topicFilter, MqttQualityOfService qos)
		{
			if (disposed) {
				throw new ObjectDisposedException (GetType ().FullName);
			}

			try {
				var packetId = packetIdProvider.GetPacketId ();
				var subscribe = new Subscribe (packetId, new Subscription (topicFilter, qos));

				var ack = default (SubscribeAck);
				var subscribeTimeout = TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs);

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
					await senderFlow.SendPublishAsync (Id, publish, packetChannel)
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
				var packetId = packetIdProvider.GetPacketId ();
				var unsubscribe = new Unsubscribe(packetId, topics);

				var ack = default (UnsubscribeAck);
				var unsubscribeTimeout = TimeSpan.FromSeconds(configuration.WaitingTimeoutSecs);

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
            await DisposeAsync (disposing: true).ConfigureAwait (continueOnCapturedContext: false);
            GC.SuppressFinalize (this);
		}

		void IDisposable.Dispose ()
		{
            DisconnectAsync ().Wait ();
		}

		protected virtual async Task DisposeAsync (bool disposing)
		{
			if (disposed) return;

			if (disposing) {
                try
                {
                    CloseClientSession ();

                    packetsSubscription.Dispose ();

                    await SendPacketAsync (new Disconnect ())
                        .ConfigureAwait (continueOnCapturedContext: false);

                    await packetListener
                        .PacketStream
                        .LastOrDefaultAsync ();

                    Close (DisconnectedReason.Disposed);
                } catch (Exception ex) {
                    Close (ex);
                } finally {
                    disposed = true;
                } 
			}
		}

		void Close (Exception ex)
		{
			tracer.Error (ex);
			receiver.OnError (ex);
			Close (DisconnectedReason.Error, ex.Message);
		}

		void Close (DisconnectedReason reason, string message = null)
		{
			tracer.Info (Properties.Resources.Client_Disposing, Id, reason);

            receiver?.OnCompleted ();
            packetsSubscription?.Dispose();
            packetListener?.Dispose();
            packetChannel?.Dispose();
            (clientSender as IDisposable)?.Dispose();
            IsConnected = false;
            Id = null;

            Disconnected (this, new MqttEndpointDisconnected (reason, message));
		}

		void OpenClientSession (string clientId, bool cleanSession)
		{
			var session = sessionRepository.Get (s => s.ClientId == clientId);
			var sessionPresent = cleanSession ? false : session != null;

			if (cleanSession && session != null) {
				sessionRepository.Delete (session);
				session = null;

				tracer.Info (Properties.Resources.Client_CleanedOldSession, clientId);
			}

			if (session == null) {
				session = new ClientSession { ClientId = clientId, Clean = cleanSession };

				sessionRepository.Create (session);

				tracer.Info (Properties.Resources.Client_CreatedSession, clientId);
			}
		}

		void CloseClientSession ()
		{
			var session = sessionRepository.Get (s => s.ClientId == Id);

			if (session == null) {
				var message = string.Format (Properties.Resources.SessionRepository_ClientSessionNotFound, Id);

				tracer.Error (message);

				throw new MqttClientException (message);
			}

			if (session.Clean) {
				sessionRepository.Delete (session);

				tracer.Info (Properties.Resources.Client_DeletedSessionOnDisconnect, Id);
			}
		}

		async Task SendPacketAsync (IPacket packet)
		{
			sender.OnNext (packet);

			await clientSender.Run (async () => await packetChannel.SendAsync (packet).ConfigureAwait (continueOnCapturedContext: false))
				.ConfigureAwait (continueOnCapturedContext: false);
		}

		void CheckUnderlyingConnection ()
		{
			if (isConnected && !packetChannel.IsConnected) {
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
