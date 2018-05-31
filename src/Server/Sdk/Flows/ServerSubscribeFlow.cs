using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal class ServerSubscribeFlow : IProtocolFlow
	{
		static readonly ITracer tracer = Tracer.Get<ServerSubscribeFlow> ();

		readonly IMqttTopicEvaluator topicEvaluator;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<RetainedMessage> retainedRepository;
		readonly IPacketIdProvider packetIdProvider;
		readonly IPublishSenderFlow senderFlow;
		readonly MqttConfiguration configuration;

		public ServerSubscribeFlow (IMqttTopicEvaluator topicEvaluator,
			IRepository<ClientSession> sessionRepository,
			IRepository<RetainedMessage> retainedRepository,
			IPacketIdProvider packetIdProvider,
			IPublishSenderFlow senderFlow,
			MqttConfiguration configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.sessionRepository = sessionRepository;
			this.retainedRepository = retainedRepository;
			this.packetIdProvider = packetIdProvider;
			this.senderFlow = senderFlow;
			this.configuration = configuration;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel)
		{
			if (input.Type != MqttPacketType.Subscribe) {
				return;
			}

			var subscribe = input as Subscribe;
			var session = sessionRepository.Read (clientId);

			if (session == null) {
				throw new MqttException (string.Format(Properties.Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			var returnCodes = new List<SubscribeReturnCode> ();

			foreach (var subscription in subscribe.Subscriptions) {
				try {
					if (!topicEvaluator.IsValidTopicFilter (subscription.TopicFilter)) {
						tracer.Error (Server.Properties.Resources.ServerSubscribeFlow_InvalidTopicSubscription, subscription.TopicFilter, clientId);

						returnCodes.Add (SubscribeReturnCode.Failure);
						continue;
					}

					var clientSubscription = session
						.GetSubscriptions()
						.FirstOrDefault(s => s.TopicFilter == subscription.TopicFilter);

					if (clientSubscription != null) {
						clientSubscription.MaximumQualityOfService = subscription.MaximumQualityOfService;
					} else {
						clientSubscription = new ClientSubscription {
							ClientId = clientId,
							TopicFilter = subscription.TopicFilter,
							MaximumQualityOfService = subscription.MaximumQualityOfService
						};

						session.AddSubscription (clientSubscription);
					}

					await SendRetainedMessagesAsync (clientSubscription, channel)
						.ConfigureAwait (continueOnCapturedContext: false);

					var supportedQos = configuration.GetSupportedQos(subscription.MaximumQualityOfService);
					var returnCode = supportedQos.ToReturnCode ();

					returnCodes.Add (returnCode);
				} catch (RepositoryException repoEx) {
					tracer.Error (repoEx, Server.Properties.Resources.ServerSubscribeFlow_ErrorOnSubscription, clientId, subscription.TopicFilter);

					returnCodes.Add (SubscribeReturnCode.Failure);
				}
			}

			sessionRepository.Update (session);

			await channel.SendAsync (new SubscribeAck (subscribe.PacketId, returnCodes.ToArray ()))
				.ConfigureAwait (continueOnCapturedContext: false);
		}

		async Task SendRetainedMessagesAsync (ClientSubscription subscription, IMqttChannel<IPacket> channel)
		{
			var retainedMessages = retainedRepository
				.ReadAll ()
				.Where (r => topicEvaluator.Matches (topicName: r.Id, topicFilter: subscription.TopicFilter));

			if (retainedMessages != null) {
				foreach (var retainedMessage in retainedMessages) {
					ushort? packetId = subscription.MaximumQualityOfService == MqttQualityOfService.AtMostOnce ?
						null : (ushort?)packetIdProvider.GetPacketId ();
					var publish = new Publish (topic: retainedMessage.Id, 
						qualityOfService: subscription.MaximumQualityOfService,
						retain: true, duplicated: false, packetId: packetId) {
						Payload = retainedMessage.Payload
					};

					await senderFlow.SendPublishAsync (subscription.ClientId, publish, channel)
						.ConfigureAwait (continueOnCapturedContext: false);
				}
			}
		}
	}
}
