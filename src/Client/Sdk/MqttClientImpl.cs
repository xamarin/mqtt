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
		static readonly ITracer tracer = Tracer.Get<MqttClientImpl>();

		bool disposed;
		bool isProtocolConnected;
		IPacketListener packetListener;
		IDisposable packetsSubscription;
		Subject<MqttApplicationMessage> receiver;

		readonly IPacketChannelFactory channelFactory;
		readonly IProtocolFlowProvider flowProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IPacketIdProvider packetIdProvider;
		readonly MqttConfiguration configuration;

		internal MqttClientImpl(IPacketChannelFactory channelFactory,
			IProtocolFlowProvider flowProvider,
			IRepositoryProvider repositoryProvider,
			IPacketIdProvider packetIdProvider,
			MqttConfiguration configuration)
		{
			receiver = new Subject<MqttApplicationMessage>();
			this.channelFactory = channelFactory;
			this.flowProvider = flowProvider;
			sessionRepository = repositoryProvider.GetRepository<ClientSession>();
			this.packetIdProvider = packetIdProvider;
			this.configuration = configuration;
		}

		public event EventHandler<MqttEndpointDisconnected> Disconnected = (sender, args) => { };

		public string Id { get; private set; }

		public bool IsConnected
		{
			get
			{
				return VerifyUnderlyingConnection();
			}
			private set
			{
				isProtocolConnected = value;
			}
		}

		public IObservable<MqttApplicationMessage> MessageStream { get { return receiver; } }

		internal IMqttChannel<IPacket> Channel { get; private set; }

		public async Task<SessionState> ConnectAsync(MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false)
		{
			if (disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			try
			{
				if (IsConnected)
				{
					throw new MqttClientException(string.Format(Properties.Resources.Client_AlreadyConnected, Id));
				}

				if (string.IsNullOrEmpty(credentials.ClientId) && !cleanSession)
				{
					throw new MqttClientException(Properties.Resources.Client_AnonymousClientWithoutCleanSession);
				}

				Id = string.IsNullOrEmpty(credentials.ClientId) ?
					MqttClient.GetAnonymousClientId() :
					credentials.ClientId;

				OpenClientSession(cleanSession);

				await InitializeChannelAsync().ConfigureAwait(continueOnCapturedContext: false);

				var connect = new Connect(Id, cleanSession)
				{
					UserName = credentials.UserName,
					Password = credentials.Password,
					Will = will,
					KeepAlive = configuration.KeepAliveSecs
				};

				await Channel.SendAsync(connect).ConfigureAwait(continueOnCapturedContext: false);

				var connectTimeout = TimeSpan.FromSeconds(configuration.WaitTimeoutSecs);
				var ack = await packetListener
					.PacketStream
					.OfType<ConnectAck>()
					.FirstOrDefaultAsync()
					.Timeout(connectTimeout);

				if (ack == null)
				{
					var message = string.Format(Properties.Resources.Client_ConnectionDisconnected, Id);

					throw new MqttClientException(message);
				}

				if (ack.Status != MqttConnectionStatus.Accepted)
				{
					throw new MqttConnectionException(ack.Status);
				}

				IsConnected = true;

				return ack.SessionPresent ? SessionState.SessionPresent : SessionState.CleanSession;
			}
			catch (TimeoutException timeEx)
			{
				await CloseAsync(timeEx).ConfigureAwait(continueOnCapturedContext: false);
				throw new MqttClientException(string.Format(Properties.Resources.Client_ConnectionTimeout, Id), timeEx);
			}
			catch (MqttConnectionException connectionEx)
			{
				await CloseAsync(connectionEx).ConfigureAwait(continueOnCapturedContext: false);

				var message = string.Format(Properties.Resources.Client_ConnectNotAccepted, Id, connectionEx.ReturnCode);

				throw new MqttClientException(message, connectionEx);
			}
			catch (MqttClientException clientEx)
			{
				await CloseAsync(clientEx).ConfigureAwait(continueOnCapturedContext: false);
				throw;
			}
			catch (Exception ex)
			{
				await CloseAsync(ex).ConfigureAwait(continueOnCapturedContext: false);
				throw new MqttClientException(string.Format(Properties.Resources.Client_ConnectionError, Id), ex);
			}
		}

		public Task<SessionState> ConnectAsync(MqttLastWill will = null) =>
			ConnectAsync(new MqttClientCredentials(), will, cleanSession: true);

		public async Task SubscribeAsync(string topicFilter, MqttQualityOfService qos)
		{
			if (disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			try
			{
				var packetId = packetIdProvider.GetPacketId();
				var subscribe = new Subscribe(packetId, new Subscription(topicFilter, qos));

				var ack = default(SubscribeAck);
				var subscribeTimeout = TimeSpan.FromSeconds(configuration.WaitTimeoutSecs);

				await Channel.SendAsync(subscribe).ConfigureAwait(continueOnCapturedContext: false);

				ack = await packetListener
					.PacketStream
					.OfType<SubscribeAck>()
					.FirstOrDefaultAsync(x => x.PacketId == packetId)
					.Timeout(subscribeTimeout);

				if (ack == null)
				{
					var message = string.Format(Properties.Resources.Client_SubscriptionDisconnected, Id, topicFilter);

					tracer.Error(message);

					throw new MqttClientException(message);
				}

				if (ack.ReturnCodes.FirstOrDefault() == SubscribeReturnCode.Failure)
				{
					var message = string.Format(Properties.Resources.Client_SubscriptionRejected, Id, topicFilter);

					tracer.Error(message);

					throw new MqttClientException(message);
				}
			}
			catch (TimeoutException timeEx)
			{
				await CloseAsync(timeEx).ConfigureAwait(continueOnCapturedContext: false);

				var message = string.Format(Properties.Resources.Client_SubscribeTimeout, Id, topicFilter);

				throw new MqttClientException(message, timeEx);
			}
			catch (MqttClientException clientEx)
			{
				await CloseAsync(clientEx).ConfigureAwait(continueOnCapturedContext: false);
				throw;
			}
			catch (Exception ex)
			{
				await CloseAsync(ex).ConfigureAwait(continueOnCapturedContext: false);

				var message = string.Format(Properties.Resources.Client_SubscribeError, Id, topicFilter);

				throw new MqttClientException(message, ex);
			}
		}

		public async Task PublishAsync(MqttApplicationMessage message, MqttQualityOfService qos, bool retain = false)
		{
			if (disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			try
			{
				ushort? packetId = qos == MqttQualityOfService.AtMostOnce ? null : (ushort?)packetIdProvider.GetPacketId();
				var publish = new Publish(message.Topic, qos, retain, duplicated: false, packetId: packetId)
				{
					Payload = message.Payload
				};

				var senderFlow = flowProvider.GetFlow<PublishSenderFlow>();

				await senderFlow
					.SendPublishAsync(Id, publish, Channel)
					.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				await CloseAsync(ex).ConfigureAwait(continueOnCapturedContext: false);
				throw;
			}
		}

		public async Task UnsubscribeAsync(params string[] topics)
		{
			if (disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			try
			{
				topics = topics ?? new string[] { };

				var packetId = packetIdProvider.GetPacketId();
				var unsubscribe = new Unsubscribe(packetId, topics);

				var ack = default(UnsubscribeAck);
				var unsubscribeTimeout = TimeSpan.FromSeconds(configuration.WaitTimeoutSecs);

				await Channel.SendAsync(unsubscribe).ConfigureAwait(continueOnCapturedContext: false);

				ack = await packetListener
					.PacketStream
					.OfType<UnsubscribeAck>()
					.FirstOrDefaultAsync(x => x.PacketId == packetId)
					.Timeout(unsubscribeTimeout);

				if (ack == null)
				{
					var message = string.Format(Properties.Resources.Client_UnsubscribeDisconnected, Id, string.Join(", ", topics));

					tracer.Error(message);

					throw new MqttClientException(message);
				}
			}
			catch (TimeoutException timeEx)
			{
				await CloseAsync(timeEx).ConfigureAwait(continueOnCapturedContext: false);

				var message = string.Format(Properties.Resources.Client_UnsubscribeTimeout, Id, string.Join(", ", topics));

				tracer.Error(message);

				throw new MqttClientException(message, timeEx);
			}
			catch (MqttClientException clientEx)
			{
				await CloseAsync(clientEx).ConfigureAwait(continueOnCapturedContext: false);
				throw;
			}
			catch (Exception ex)
			{
				await CloseAsync(ex).ConfigureAwait(continueOnCapturedContext: false);

				var message = string.Format(Properties.Resources.Client_UnsubscribeError, Id, string.Join(", ", topics));

				tracer.Error(message);

				throw new MqttClientException(message, ex);
			}
		}

		public async Task DisconnectAsync()
		{
			try
			{
				if (!IsConnected)
				{
					throw new MqttClientException(Properties.Resources.Client_AlreadyDisconnected);
				}

				packetsSubscription?.Dispose();

				await Channel.SendAsync(new Disconnect()).ConfigureAwait(continueOnCapturedContext: false);

				await packetListener
					.PacketStream
					.LastOrDefaultAsync();

				await CloseAsync(DisconnectedReason.SelfDisconnected).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				await CloseAsync(ex).ConfigureAwait(continueOnCapturedContext: false);
			}
		}

		void IDisposable.Dispose()
		{
			DisposeAsync(disposing: true).Wait();
			GC.SuppressFinalize(this);
		}

		protected virtual async Task DisposeAsync(bool disposing)
		{
			if (disposed) return;

			if (disposing)
			{
				if (IsConnected)
				{
					await DisconnectAsync().ConfigureAwait(continueOnCapturedContext: false);
				}

				disposed = true;
			}
		}

		async Task CloseAsync(Exception ex)
		{
			tracer.Error(ex);
			await CloseAsync(DisconnectedReason.Error, ex.Message).ConfigureAwait(continueOnCapturedContext: false);
		}

		async Task CloseAsync(DisconnectedReason reason, string message = null)
		{
			tracer.Info(Properties.Resources.Client_Closing, Id, reason);

			CloseClientSession();
			packetsSubscription?.Dispose();
			packetListener?.Dispose();
			ResetReceiver();

			if(Channel != null)
			{
				await Channel.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			
			IsConnected = false;
			Id = null;

			Disconnected(this, new MqttEndpointDisconnected(reason, message));
		}

		async Task InitializeChannelAsync()
		{
			Channel = await channelFactory
				.CreateAsync()
				.ConfigureAwait(continueOnCapturedContext: false);

			packetListener = new ClientPacketListener(Channel, flowProvider, configuration);
			packetListener.Listen();
			ObservePackets();
		}

		void OpenClientSession(bool cleanSession)
		{
			var session = string.IsNullOrEmpty(Id) ? default(ClientSession) : sessionRepository.Read(Id);

			if (cleanSession && session != null)
			{
				sessionRepository.Delete(session.Id);
				session = null;

				tracer.Info(Properties.Resources.Client_CleanedOldSession, Id);
			}

			if (session == null)
			{
				session = new ClientSession(Id, cleanSession);

				sessionRepository.Create(session);

				tracer.Info(Properties.Resources.Client_CreatedSession, Id);
			}
		}

		void CloseClientSession()
		{
			var session = string.IsNullOrEmpty(Id) ? default(ClientSession) : sessionRepository.Read(Id);

			if (session == null)
			{
				return;
			}

			if (session.Clean)
			{
				sessionRepository.Delete(session.Id);

				tracer.Info(Properties.Resources.Client_DeletedSessionOnDisconnect, Id);
			}
		}

		bool VerifyUnderlyingConnection()
		{
			if (isProtocolConnected && !Channel.IsConnected)
			{
				CloseAsync(DisconnectedReason.Error, Properties.Resources.Client_UnexpectedChannelDisconnection).FireAndForget();
			}

			return isProtocolConnected && Channel.IsConnected;
		}

		void ObservePackets()
		{
			packetsSubscription = packetListener
				.PacketStream
				.Subscribe(packet =>
				{
					if (packet.Type == MqttPacketType.Publish)
					{
						var publish = packet as Publish;
						var message = new MqttApplicationMessage(publish.Topic, publish.Payload);

						receiver.OnNext(message);
						tracer.Info(Properties.Resources.Client_NewApplicationMessageReceived, Id, publish.Topic);
					}
				}, ex =>
				{
					CloseAsync(ex).FireAndForget();
				}, () =>
				{
					tracer.Warn(Properties.Resources.Client_PacketsObservableCompleted);
					CloseAsync(DisconnectedReason.RemoteDisconnected).FireAndForget();
				});
		}

		void ResetReceiver()
		{
			receiver?.OnCompleted();
			receiver = new Subject<MqttApplicationMessage>();
		}
	}
}
