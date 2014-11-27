using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes
{
    public class Client : IClient
    {
		readonly Subject<ApplicationMessage> receiver = new Subject<ApplicationMessage> ();
		readonly Subject<IPacket> sender = new Subject<IPacket> ();

		readonly IChannel<IPacket> channel;
		readonly IObservable<Unit> timeListener;
		readonly ProtocolConfiguration configuration;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;

		readonly IDictionary<ushort, IDisposable> packetTimers;

		IDisposable keepAliveTimer;
		IDisposable connectTimer;

        public Client(IChannel<IPacket> channel, IObservable<Unit> timeListener, ProtocolConfiguration configuration, 
			IRepository<ClientSession> sessionRepository,
			IRepository<PacketIdentifier> packetIdentifierRepository)
        {
			this.channel = channel;
			this.timeListener = timeListener;
			this.configuration = configuration;
			this.sessionRepository = sessionRepository;
			this.packetIdentifierRepository = packetIdentifierRepository;

			this.packetTimers = new Dictionary<ushort, IDisposable> ();

			this.channel.Receiver.OfType<ConnectAck> ().Subscribe (connectAck => {
				this.connectTimer.Dispose ();
			});

			this.channel.Receiver.OfType<Publish>().Subscribe (publish => {
				var message = new ApplicationMessage (publish.Topic, publish.Payload);

				this.receiver.OnNext (message);
			});

			this.channel.Receiver.OfType<PublishAck>().Subscribe (publishAck => {
				this.StopPacketTimer (publishAck.PacketId);
			});

			this.channel.Receiver.OfType<PublishComplete>().Subscribe (publishComplete => {
				this.StopPacketTimer (publishComplete.PacketId);
			});

			this.channel.Receiver.OfType<SubscribeAck>().Subscribe (subscribeAck => {
				this.StopPacketTimer (subscribeAck.PacketId);
			});

			this.channel.Receiver.OfType<UnsubscribeAck>().Subscribe (unsubscribeAck => {
				this.StopPacketTimer (unsubscribeAck.PacketId);
			});

			this.channel.Receiver.Subscribe (_ => { }, ex => { this.IsConnected = false; this.Id = null; });
        }

		public string Id { get; private set; }

		public bool IsConnected { get; private set; }

		public IObservable<ApplicationMessage> Receiver { get { return this.receiver; } }

		public IObservable<IPacket> Sender { get { return this.sender; } }

		public async Task ConnectAsync (ClientCredentials credentials, bool cleanSession = false)
		{
			await this.ConnectAsync (credentials, null, cleanSession);
		}

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

			this.Id = credentials.ClientId;

			this.connectTimer = this.timeListener.Skip (this.configuration.WaitingTimeoutSecs).Take (1).Subscribe (_ => {
				this.channel.Close ();
			});
		}

		public async Task SubscribeAsync (string topicFilter, QualityOfService qos)
		{
			var packetId = this.packetIdentifierRepository.GetUnusedPacketIdentifier(new Random());
			var subscribe = new Subscribe (packetId, new Subscription (topicFilter, qos));

			await this.SendPacket (subscribe);

			this.packetTimers.Add (packetId, this.GetTimer());
		}

		public async Task PublishAsync (ApplicationMessage message, QualityOfService qos, bool retain = false)
		{
			var packetId = this.packetIdentifierRepository.GetPacketIdentifier(qos);
			var publish = new Publish (message.Topic, qos, retain, duplicated: false, packetId: packetId)
			{
				Payload = message.Payload
			};

			await this.SendPacket (publish);

			if (packetId.HasValue) {
				this.packetTimers.Add (packetId.Value, this.GetTimer());
			}
		}

		public async Task UnsubscribeAsync (params string[] topics)
		{
			var packetId = this.packetIdentifierRepository.GetUnusedPacketIdentifier(new Random());
			var unsubscribe = new Unsubscribe(packetId, topics);

			await this.SendPacket (unsubscribe);

			this.packetTimers.Add (packetId, this.GetTimer());
		}

		public async Task DisconnectAsync ()
		{
			this.CloseClientSession ();

			var disconnect = new Disconnect ();

			await this.SendPacket (disconnect);
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
			this.StopKeepAliveTimer ();

			await this.channel.SendAsync (packet);
			this.sender.OnNext (packet);

			this.StartKeepAliveTimer ();
		}

		private IDisposable GetTimer()
		{
			return this.timeListener.Skip (this.configuration.WaitingTimeoutSecs).Take (1).Subscribe (_ => {
				this.channel.Close ();
			});
		}

		private void StopPacketTimer(ushort packetId)
		{
			var timer = default (IDisposable);

			if (!this.packetTimers.TryGetValue (packetId, out timer))
				return;

			timer.Dispose ();
			this.packetTimers.Remove (packetId);
		}

		private void StartKeepAliveTimer()
		{
			if (configuration.KeepAliveSecs == 0)
				return;

			this.keepAliveTimer = this.timeListener.Skip (configuration.KeepAliveSecs).Take (1).Subscribe (async _ => {
				var ping = new PingRequest ();

				await this.channel.SendAsync(ping);
			});
		}

		private void StopKeepAliveTimer()
		{
			if (this.keepAliveTimer == default(IDisposable))
				return;

			this.keepAliveTimer.Dispose ();
		}
	}
}
