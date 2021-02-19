using System.Diagnostics;
using System.Linq;
using System.Net.Mqtt.Sdk.Packets;
using System.Net.Mqtt.Sdk.Storage;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using ServerProperties = System.Net.Mqtt.Server.Properties;

namespace System.Net.Mqtt.Sdk.Flows
{
	internal class ServerPublishReceiverFlow : PublishReceiverFlow, IServerPublishReceiverFlow
    {
		static readonly ITracer tracer = Tracer.Get<ServerPublishReceiverFlow> ();

		readonly IConnectionProvider connectionProvider;
		readonly IPublishSenderFlow senderFlow;
		readonly IRepository<ConnectionWill> willRepository;
		readonly IPacketIdProvider packetIdProvider;
		readonly ISubject<MqttUndeliveredMessage> undeliveredMessagesListener;

		public ServerPublishReceiverFlow (IMqttTopicEvaluator topicEvaluator,
            IConnectionProvider connectionProvider,
			IPublishSenderFlow senderFlow,
			IRepository<RetainedMessage> retainedRepository,
			IRepository<ClientSession> sessionRepository,
			IRepository<ConnectionWill> willRepository,
			IPacketIdProvider packetIdProvider,
            ISubject<MqttUndeliveredMessage> undeliveredMessagesListener,
			MqttConfiguration configuration)
			: base (topicEvaluator, retainedRepository, sessionRepository, configuration)
		{
			this.connectionProvider = connectionProvider;
			this.senderFlow = senderFlow;
			this.willRepository = willRepository;
			this.packetIdProvider = packetIdProvider;
			this.undeliveredMessagesListener = undeliveredMessagesListener;
		}

        public async Task SendWillAsync(string clientId)
        {
            var will = willRepository.Read(clientId);

            if (will != null && will.Will != null)
            {
                var willPublish = new Publish(will.Will.Topic, will.Will.QualityOfService, will.Will.Retain, duplicated: false)
                {
                    Payload = will.Will.Payload
                };

                tracer.Info(Server.Properties.Resources.ServerPublishReceiverFlow_SendingWill, clientId, willPublish.Topic);

                await DispatchAsync(willPublish, clientId, isWill: true)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        protected override async Task ProcessPublishAsync (Publish publish, string clientId)
		{
			if (publish.Retain) {
				var existingRetainedMessage = retainedRepository.Read (publish.Topic);

				if (existingRetainedMessage != null) {
					retainedRepository.Delete (existingRetainedMessage.Id);
				}

				if (publish.Payload.Length > 0) {
					var retainedMessage = new RetainedMessage (publish.Topic,
						publish.QualityOfService,
						publish.Payload);

					retainedRepository.Create (retainedMessage);
				}
			}

			await DispatchAsync (publish, clientId)
				.ConfigureAwait (continueOnCapturedContext: false);
		}

        protected override void Validate (Publish publish, string clientId)
        {
            base.Validate (publish, clientId);

            if (publish.Topic.Trim ().StartsWith ("$") && !connectionProvider.PrivateClients.Contains (clientId))
            {
                throw new MqttException (ServerProperties.Resources.ServerPublishReceiverFlow_SystemMessageNotAllowedForClient);
            }
        }

		async Task DispatchAsync (Publish publish, string clientId, bool isWill = false)
		{
			var subscriptions = sessionRepository
				.ReadAll ().ToList ()
				.SelectMany (s => s.GetSubscriptions ())
				.Where (x => topicEvaluator.Matches (publish.Topic, x.TopicFilter));

			if (!subscriptions.Any ()) {
				tracer.Verbose (Server.Properties.Resources.ServerPublishReceiverFlow_TopicNotSubscribed, publish.Topic, clientId);

				undeliveredMessagesListener.OnNext (new MqttUndeliveredMessage { SenderId = clientId, Message = new MqttApplicationMessage (publish.Topic, publish.Payload) });
			} else {
				foreach (var subscription in subscriptions) {
					await DispatchAsync (publish, subscription, isWill)
						.ConfigureAwait (continueOnCapturedContext: false);
				}
			}
		}

		async Task DispatchAsync (Publish publish, ClientSubscription subscription, bool isWill = false)
		{
			var requestedQos = isWill ? publish.QualityOfService : subscription.MaximumQualityOfService;
			var supportedQos = configuration.GetSupportedQos(requestedQos);
			var retain = isWill ? publish.Retain : false;
			ushort? packetId = supportedQos == MqttQualityOfService.AtMostOnce ? null : (ushort?)packetIdProvider.GetPacketId ();
			var subscriptionPublish = new Publish (publish.Topic, supportedQos, retain, duplicated: false, packetId: packetId) {
				Payload = publish.Payload
			};
			var clientChannel = await connectionProvider
				.GetConnectionAsync (subscription.ClientId)
				.ConfigureAwait(continueOnCapturedContext: false);

			await senderFlow.SendPublishAsync (subscription.ClientId, subscriptionPublish, clientChannel)
				.ConfigureAwait (continueOnCapturedContext: false);
		}
	}
}
