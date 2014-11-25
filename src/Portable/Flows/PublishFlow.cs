using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class PublishFlow : IProtocolFlow
	{
		readonly ProtocolConfiguration configuration;
		readonly IClientManager clientManager;
		readonly ITopicEvaluator topicEvaluator;
		readonly IRepository<RetainedMessage> retainedRepository;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;
		readonly IDictionary<QualityOfService, Func<Publish, IPacket>> publishRules;

		public PublishFlow (ProtocolConfiguration configuration, IClientManager clientManager, ITopicEvaluator topicEvaluator,
			IRepository<RetainedMessage> retainedRepository, IRepository<ClientSession> sessionRepository, 
			IRepository<PacketIdentifier> packetIdentifierRepository)
		{
			this.configuration = configuration;
			this.clientManager = clientManager;
			this.topicEvaluator = topicEvaluator;
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

			if (publish == null) {
				var error = string.Format (Resources.ProtocolFlow_InvalidPacketType, input.Type, "Publish");

				throw new ProtocolException(error);
			}

			if (publish.Retain) {
				var existingRetainedMessage = this.retainedRepository.Get(r => r.Topic == publish.Topic);

				if(existingRetainedMessage != null) {
					this.retainedRepository.Delete(existingRetainedMessage);
				}

				if (publish.Payload.Length > 0) {
					var retainedMessage = new RetainedMessage {
						Topic = publish.Topic,
						QualityOfService = publish.QualityOfService,
						Payload = publish.Payload
					};

					this.retainedRepository.Create(retainedMessage);
				}
			}

			var qos = publish.QualityOfService > this.configuration.MaximumQualityOfService ? 
				this.configuration.MaximumQualityOfService : 
				publish.QualityOfService;
			var sessions = this.sessionRepository.GetAll (); //TODO: Needs some refactoring over Repository and Storage strategy to try not doing this on memory (right now It's an IQueryable but doesn't work like that)
			var subscriptions = sessions.SelectMany(s => s.Subscriptions).Where(x => this.topicEvaluator.Matches(publish.Topic, x.TopicFilter));

			foreach (var subscription in subscriptions) {
				var requestedQos = subscription.MaximumQualityOfService > this.configuration.MaximumQualityOfService ? 
					this.configuration.MaximumQualityOfService : 
					subscription.MaximumQualityOfService;
				var packetId = this.packetIdentifierRepository.GetPacketIdentifier (requestedQos);

				//TODO: Figure out how to control the reception of a message and the duplicate delivery in case the ack is not received
				var subscriptionPublish = new Publish (publish.Topic, requestedQos, retain: false, duplicatedDelivery: false, packetId: packetId) {
					Payload = publish.Payload
				};

				await this.clientManager.SendMessageAsync (subscription.ClientId, subscriptionPublish);
			}

			var rule = default (Func<Publish, IPacket>);

			this.publishRules.TryGetValue (qos, out rule);

			var result = rule (publish);

			if (result != default (IPacket)) {
				await this.SendAckAsync (result, channel);
			}
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
			await channel.SendAsync(ack);
		}
	}
}
