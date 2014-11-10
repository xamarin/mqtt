using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class PublishFlow : IProtocolFlow
	{
		readonly IProtocolConfiguration configuration;
		readonly IClientManager clientManager;
		readonly IRepository<RetainedMessage> retainedRepository;
		readonly IRepository<ClientSubscription> subscriptionRepository;
		readonly IDictionary<QualityOfService, Func<Publish, IPacket>> publishRules;

		public PublishFlow (IProtocolConfiguration configuration, IClientManager clientManager, IRepository<RetainedMessage> retainedRepository,  IRepository<ClientSubscription> subscriptionRepository)
		{
			this.configuration = configuration;
			this.clientManager = clientManager;
			this.retainedRepository = retainedRepository;
			this.subscriptionRepository = subscriptionRepository;

			this.publishRules = new Dictionary<QualityOfService, Func<Publish, IPacket>> ();

			this.publishRules.Add (QualityOfService.AtMostOnce, RunQoS0Flow);
			this.publishRules.Add (QualityOfService.AtLeastOnce, RunQoS1Flow);
			this.publishRules.Add (QualityOfService.ExactlyOnce, RunQoS2Flow);
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type == PacketType.PublishAck || input.Type == PacketType.PublishComplete)
				return;

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
			var subscriptions = this.subscriptionRepository.GetAll(s => s.Matches(publish.Topic));

			foreach (var subscription in subscriptions) {
				var requestedQos = subscription.RequestedQualityOfService > this.configuration.SupportedQualityOfService ? 
					this.configuration.SupportedQualityOfService : 
					subscription.RequestedQualityOfService;
				//TODO: Generate Packet Id taking into account already used Packet Ids
				ushort? packetId = requestedQos == QualityOfService.AtMostOnce ? null : (ushort?)new Random ().Next (0, ushort.MaxValue);

				var subscriptionPublish = new Publish (publish.Topic, requestedQos, retain: false, duplicatedDelivery: false, packetId: packetId);

				await this.clientManager.SendMessageAsync (subscription.ClientId, subscriptionPublish);
			}

			var rule = default (Func<Publish, IPacket>);

			this.publishRules.TryGetValue (qos, out rule);

			var result = rule (publish);

			await this.SendAckAsync (result, channel);
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
