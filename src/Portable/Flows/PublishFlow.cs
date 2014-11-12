using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;
using System.Linq;

namespace Hermes.Flows
{
	public class PublishFlow : IProtocolFlow
	{
		readonly IProtocolConfiguration configuration;
		readonly IClientManager clientManager;
		readonly IRepository<RetainedMessage> retainedRepository;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;
		readonly IDictionary<QualityOfService, Func<Publish, IPacket>> publishRules;

		public PublishFlow (IProtocolConfiguration configuration, IClientManager clientManager,
			IRepository<RetainedMessage> retainedRepository, IRepository<ClientSession> sessionRepository, 
			IRepository<PacketIdentifier> packetIdentifierRepository)
		{
			this.configuration = configuration;
			this.clientManager = clientManager;
			this.retainedRepository = retainedRepository;
			this.sessionRepository = sessionRepository;
			this.packetIdentifierRepository = packetIdentifierRepository;

			this.publishRules = new Dictionary<QualityOfService, Func<Publish, IPacket>> ();

			this.publishRules.Add (QualityOfService.AtMostOnce, RunQoS0Flow);
			this.publishRules.Add (QualityOfService.AtLeastOnce, RunQoS1Flow);
			this.publishRules.Add (QualityOfService.ExactlyOnce, RunQoS2Flow);
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type == PacketType.PublishAck) {
				var publishAck = input as PublishAck;

				this.packetIdentifierRepository.Delete (i => i.Value == publishAck.PacketId);

				return;
			}

			if (input.Type == PacketType.PublishComplete) {
				var publishComplete = input as PublishComplete;

				this.packetIdentifierRepository.Delete (i => i.Value == publishComplete.PacketId);

				return;
			}

			if (input.Type != PacketType.Publish && input.Type != PacketType.PublishReceived && 
				input.Type != PacketType.PublishRelease) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Publish");

				throw new ProtocolException(error);
			}

			if (input.Type == PacketType.PublishReceived) {
				var publishReceived = input as PublishReceived;
				var ack = this.RunQoS2Flow (publishReceived);

				await this.SendAckAsync (ack, channel);
				return;
			}

			if (input.Type == PacketType.PublishRelease) {
				var publishRelease = input as PublishRelease;
				var ack = this.RunQoS2Flow (publishRelease);

				await this.SendAckAsync (ack, channel);
				return;
			}

			var publish = input as Publish;

			if (publish.Retain) {
				var existingRetainedMessage = this.retainedRepository.Get(r => r.Topic == publish.Topic);

				if(existingRetainedMessage != null) {
					this.retainedRepository.Delete(existingRetainedMessage);
				}

				if (publish.Payload.Length > 0) {
					var retainedMessage = new RetainedMessage {
						Topic = publish.Topic,
						QualityOfService = publish.QualityOfService,
						Payload = Encoding.UTF8.GetString(publish.Payload, 0, publish.Payload.Length)
					};

					this.retainedRepository.Create(retainedMessage);
				}
			}

			var qos = publish.QualityOfService > this.configuration.SupportedQualityOfService ? 
				this.configuration.SupportedQualityOfService : 
				publish.QualityOfService;
			var sessions = this.sessionRepository.GetAll (); //TODO: Needs some refactoring over Repository and Storage strategy to try not doing this on memory (right now It's an IQueryable but doesn't work like that)
			var subscriptions = sessions.SelectMany(s => s.Subscriptions).Where(x => x.Matches(publish.Topic));

			foreach (var subscription in subscriptions) {
				var requestedQos = subscription.MaximumQualityOfService > this.configuration.SupportedQualityOfService ? 
					this.configuration.SupportedQualityOfService : 
					subscription.MaximumQualityOfService;
				var packetId = this.GetPacketIdentifier (requestedQos);

				var subscriptionPublish = new Publish (publish.Topic, requestedQos, retain: false, duplicatedDelivery: false, packetId: packetId);

				await this.clientManager.SendMessageAsync (subscription.ClientId, subscriptionPublish);
			}

			var rule = default (Func<Publish, IPacket>);

			this.publishRules.TryGetValue (qos, out rule);

			var result = rule (publish);

			await this.SendAckAsync (result, channel);
		}

		private ushort? GetPacketIdentifier(QualityOfService qos)
		{
			var packetId = default (ushort?);

			if(qos != QualityOfService.AtMostOnce) {
				packetId = this.GetUnusedPacketIdentifier (new Random ());
			}

			return packetId;
		}

		private ushort GetUnusedPacketIdentifier(Random random)
		{
			var packetId = (ushort)random.Next (1, ushort.MaxValue);

			if (this.packetIdentifierRepository.Exist (i => i.Value == packetId)) {
				packetId = this.GetUnusedPacketIdentifier (random);
			}

			return packetId;
		}

		private IPacket RunQoS0Flow (Publish publish)
		{
			return default (IPacket);
		}

		private IPacket RunQoS1Flow (Publish publish)
		{
			if(!publish.PacketId.HasValue)
				throw new ProtocolException(Resources.PublishFlow_PacketIdRequired);

			return new PublishAck (publish.PacketId.Value);
		}

		private IPacket RunQoS2Flow (Publish publish)
		{
			if(!publish.PacketId.HasValue)
				throw new ProtocolException(Resources.PublishFlow_PacketIdRequired);

			return new PublishReceived (publish.PacketId.Value);
		}

		private IPacket RunQoS2Flow (PublishReceived publishReceived)
		{
			return new PublishRelease(publishReceived.PacketId);
		}

		private IPacket RunQoS2Flow (PublishRelease publishRelease)
		{
			return new PublishComplete(publishRelease.PacketId);
		}

		private async Task SendAckAsync(IPacket ack, IChannel<IPacket> channel) 
		{
			if (ack == default (IPacket))
				return;

			await channel.SendAsync(ack);
		}
	}
}
