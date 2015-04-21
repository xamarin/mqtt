using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermes.Diagnostics;
using Hermes.Exceptions;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerSubscribeFlow : IProtocolFlow
	{
		static readonly ITracer tracer = Tracer.Get<ServerSubscribeFlow> ();

		readonly ITopicEvaluator topicEvaluator;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;
		readonly IRepository<RetainedMessage> retainedRepository;
		readonly IPublishSenderFlow senderFlow;
		readonly ProtocolConfiguration configuration;

		public ServerSubscribeFlow (ITopicEvaluator topicEvaluator, 
			IRepository<ClientSession> sessionRepository, 
			IRepository<PacketIdentifier> packetIdentifierRepository, 
			IRepository<RetainedMessage> retainedRepository,
			IPublishSenderFlow senderFlow,
			ProtocolConfiguration configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.sessionRepository = sessionRepository;
			this.packetIdentifierRepository = packetIdentifierRepository;
			this.retainedRepository = retainedRepository;
			this.senderFlow = senderFlow;
			this.configuration = configuration;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			if (input.Type != PacketType.Subscribe) {
				return;
			}

			var subscribe = input as Subscribe;
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new ProtocolException (string.Format(Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			var returnCodes = new List<SubscribeReturnCode> ();

			foreach (var subscription in subscribe.Subscriptions) {
				try {
					if (!this.topicEvaluator.IsValidTopicFilter (subscription.TopicFilter)) {
						tracer.Error(Resources.Tracer_ServerSubscribeFlow_InvalidTopicSubscription, subscription.TopicFilter, clientId);

						returnCodes.Add (SubscribeReturnCode.Failure);
						continue;
					}

					var clientSubscription = session.Subscriptions.FirstOrDefault(s => s.TopicFilter == subscription.TopicFilter);

					if (clientSubscription != null) {
						clientSubscription.MaximumQualityOfService = subscription.MaximumQualityOfService;
					} else {
						clientSubscription = new ClientSubscription { 
							ClientId = clientId,
							TopicFilter = subscription.TopicFilter,
							MaximumQualityOfService = subscription.MaximumQualityOfService 
						};

						session.Subscriptions.Add (clientSubscription);
					}

					await this.SendRetainedMessagesAsync (clientSubscription, channel);
		
					var supportedQos = configuration.GetSupportedQos(subscription.MaximumQualityOfService);
					var returnCode = supportedQos.ToReturnCode ();

					returnCodes.Add (returnCode);
				} catch (RepositoryException repoEx) {
					tracer.Error(repoEx, Resources.Tracer_ServerSubscribeFlow_ErrorOnSubscription, clientId, subscription.TopicFilter);

					returnCodes.Add (SubscribeReturnCode.Failure);
				}
			}

			this.sessionRepository.Update (session);

			await channel.SendAsync(new SubscribeAck (subscribe.PacketId, returnCodes.ToArray()));
		}

		private async Task SendRetainedMessagesAsync(ClientSubscription subscription, IChannel<IPacket> channel)
		{
			var retainedMessages = this.retainedRepository.GetAll ()
				.Where(r => this.topicEvaluator.Matches(r.Topic, subscription.TopicFilter));

			if (retainedMessages != null) {
				foreach (var retainedMessage in retainedMessages) {
					var packetId = this.packetIdentifierRepository.GetPacketIdentifier (subscription.MaximumQualityOfService);
					var publish = new Publish (retainedMessage.Topic, subscription.MaximumQualityOfService, 
						retain: true, duplicated: false, packetId: packetId) {
						Payload = retainedMessage.Payload
					};

					await this.senderFlow.SendPublishAsync(subscription.ClientId, publish, channel);
				}
			}
		}
	}
}
