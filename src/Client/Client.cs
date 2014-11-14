using System;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Hermes
{
    public class Client : IClient
    {
		readonly IChannel<IPacket> channel;
		readonly IObservable<Unit> timeListener;
		readonly IProtocolConfiguration configuration;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;

        public Client(IChannel<IPacket> channel, IObservable<Unit> timeListener, IProtocolConfiguration configuration, IRepository<PacketIdentifier> packetIdentifierRepository)
        {
			this.channel = channel;
			this.timeListener = timeListener;
			this.configuration = configuration;
			this.packetIdentifierRepository = packetIdentifierRepository;

			this.channel.Receiver.Subscribe (packet => {

			});
        }

		public string Id { get; private set; }

		public bool IsConnected { get; private set; }

		public IObservable<ApplicationMessage> Receiver
		{
			get { throw new NotImplementedException (); }
		}

		public IObservable<IPacket> Sender
		{
			get { throw new NotImplementedException (); }
		}

		public async Task ConnectAsync (ClientCredentials credentials, bool cleanSession = false)
		{
			var connect = new Connect (credentials.ClientId, cleanSession) {
				UserName = credentials.UserName,
				Password = credentials.Password,
				KeepAlive = this.configuration.KeepAlive
			};

			await this.channel.SendAsync (connect);

			this.Id = credentials.ClientId;
		}

		public async Task ConnectAsync (ClientCredentials credentials, Will will, bool cleanSession = false)
		{
			var connect = new Connect (credentials.ClientId, cleanSession) {
				UserName = credentials.UserName,
				Password = credentials.Password,
				Will = will,
				KeepAlive = this.configuration.KeepAlive
			};

			await this.channel.SendAsync (connect);
		}

		public async Task SubscribeAsync (string topicFilter)
		{
			await this.SubscribeAsync (topicFilter, this.configuration.SupportedQualityOfService);
		}

		public async Task SubscribeAsync (string topicFilter, QualityOfService qos)
		{
			var packetId = this.packetIdentifierRepository.GetUnusedPacketIdentifier(new Random());
			var subscribe = new Subscribe (packetId, new Subscription (topicFilter, qos));

			await this.channel.SendAsync (subscribe);
		}

		public async Task PublishAsync (ApplicationMessage message, bool retain = false)
		{
			var packetId = this.packetIdentifierRepository.GetPacketIdentifier(this.configuration.SupportedQualityOfService);
			var publish = new Publish (message.Topic, this.configuration.SupportedQualityOfService, retain, duplicatedDelivery: false, packetId: packetId);

			await this.channel.SendAsync (publish);
		}

		public async Task UnsubscribeAsync (params string[] topics)
		{
			var packetId = this.packetIdentifierRepository.GetUnusedPacketIdentifier(new Random());
			var unsubscribe = new Unsubscribe(packetId, topics);

			await this.channel.SendAsync (unsubscribe);
		}

		public async Task DisconnectAsync ()
		{
			var disconnect = new Disconnect ();

			await this.channel.SendAsync (disconnect);
		}
	}
}
