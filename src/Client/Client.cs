using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Diagnostics;
using Hermes.Flows;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes
{
    public class Client : IClient
    {
		static readonly ITracer tracer = Tracer.Get<Client> ();

		readonly Subject<ApplicationMessage> receiver = new Subject<ApplicationMessage> ();
		readonly Subject<IPacket> sender = new Subject<IPacket> ();

		readonly IChannel<IPacket> protocolChannel;
		readonly IProtocolFlowProvider flowProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;
		readonly ProtocolConfiguration configuration;

        public Client(IBufferedChannel<byte> socket, 
			IPacketChannelFactory channelFactory, 
			IPacketChannelAdapter channelAdapter,
			IProtocolFlowProvider flowProvider,
			IRepositoryProvider repositoryProvider,
			ProtocolConfiguration configuration)
        {
			var channel = channelFactory.CreateChannel (socket);

			this.protocolChannel = channelAdapter.Adapt (channel);
			this.flowProvider = flowProvider;
			this.sessionRepository = repositoryProvider.GetRepository<ClientSession>();
			this.packetIdentifierRepository = repositoryProvider.GetRepository<PacketIdentifier>();
			this.configuration = configuration;

			this.protocolChannel.Receiver.OfType<Publish>().Subscribe (publish => {
				var message = new ApplicationMessage (publish.Topic, publish.Payload);

				this.receiver.OnNext (message);
			});

			this.protocolChannel.Sender
				.Subscribe (_ => { }, 
					ex => { 
						tracer.Error (ex);
						this.receiver.OnError (ex);
						this.sender.OnError (ex);
						this.CloseChannel ();
					}, () => {
						this.receiver.OnCompleted ();
						this.sender.OnCompleted ();
						this.CloseChannel ();
					});

			this.protocolChannel.Receiver
				.Subscribe (_ => { }, 
					ex => { 
						tracer.Error (ex);
						this.receiver.OnError (ex);
						this.sender.OnError (ex);
						this.CloseChannel ();
					}, () => {
						this.receiver.OnCompleted ();
						this.sender.OnCompleted();
						this.CloseChannel ();
					});
        }

		public string Id { get; private set; }

		public bool IsConnected { get; private set; }

		public IObservable<ApplicationMessage> Receiver { get { return this.receiver; } }

		public IObservable<IPacket> Sender { get { return this.sender; } }

		/// <exception cref="ClientException">ClientException</exception>
		public async Task ConnectAsync (ClientCredentials credentials, bool cleanSession = false)
		{
			await this.ConnectAsync (credentials, null, cleanSession);
		}

		/// <exception cref="ClientException">ClientException</exception>
		public async Task ConnectAsync (ClientCredentials credentials, Will will, bool cleanSession = false)
		{
			this.OpenClientSession (credentials.ClientId, cleanSession);

			var connect = new Connect (credentials.ClientId, cleanSession) {
				UserName = credentials.UserName,
				Password = credentials.Password,
				Will = will,
				KeepAlive = this.configuration.KeepAliveSecs
			};

			await this.SendPacket (connect);

			var connectTimeout = new TimeSpan(0, 0, this.configuration.WaitingTimeoutSecs);
			var ack = default (ConnectAck);

			try {
				ack = await this.protocolChannel.Receiver
					.OfType<ConnectAck> ()
					.FirstOrDefaultAsync ()
					.Timeout(connectTimeout);
			} catch(TimeoutException timeEx) {
				throw new ClientException (Resources.Client_ConnectionTimeout, timeEx);
			} catch (Exception ex) {
				throw new ClientException (Resources.Client_ConnectionError, ex);
			}

			if (ack == null)
				throw new ClientException (Resources.Client_ConnectionDisconnected);

			this.Id = credentials.ClientId;
			this.IsConnected = true;
		}

		public async Task SubscribeAsync (string topicFilter, QualityOfService qos)
		{
			var packetId = this.packetIdentifierRepository.GetUnusedPacketIdentifier(new Random());
			var subscribe = new Subscribe (packetId, new Subscription (topicFilter, qos));

			await this.SendPacket (subscribe);
		}

		public async Task PublishAsync (ApplicationMessage message, QualityOfService qos, bool retain = false)
		{
			var packetId = this.packetIdentifierRepository.GetPacketIdentifier(qos);
			var publish = new Publish (message.Topic, qos, retain, duplicated: false, packetId: packetId)
			{
				Payload = message.Payload
			};

			var senderFlow = this.flowProvider.GetFlow<PublishSenderFlow> ();

			await senderFlow.SendPublishAsync (this.Id, publish, this.protocolChannel);
		}

		public async Task UnsubscribeAsync (params string[] topics)
		{
			var packetId = this.packetIdentifierRepository.GetUnusedPacketIdentifier(new Random());
			var unsubscribe = new Unsubscribe(packetId, topics);

			await this.SendPacket (unsubscribe);
		}

		public async Task DisconnectAsync ()
		{
			this.CloseClientSession ();

			var disconnect = new Disconnect ();

			await this.SendPacket (disconnect);

			this.CloseChannel ();
		}

		private void OpenClientSession(string clientId, bool cleanSession)
		{
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);
			var sessionPresent = cleanSession ? false : session != null;

			if (cleanSession && session != null) {
				this.sessionRepository.Delete(session);
				session = null;
			}

			if (session == null) {
				session = new ClientSession { ClientId = clientId, Clean = cleanSession };

				this.sessionRepository.Create (session);
			}
		}

		private void CloseClientSession()
		{
			var session = this.sessionRepository.Get (s => s.ClientId == this.Id);

			if (session.Clean) {
				this.sessionRepository.Delete (session);
			}
		}

		private async Task SendPacket(IPacket packet)
		{
			await this.protocolChannel.SendAsync (packet);
			this.sender.OnNext (packet);
		}

		private void CloseChannel ()
		{
			this.IsConnected = false; 
			this.Id = null;
			this.protocolChannel.Close ();
		}
	}
}
