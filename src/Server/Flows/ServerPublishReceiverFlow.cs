using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using System.Net.Mqtt.Server;
using Props = System.Net.Mqtt.Server.Properties;

namespace System.Net.Mqtt.Flows
{
	internal class ServerPublishReceiverFlow : PublishReceiverFlow
	{
		static readonly ITracer tracer = Tracer.Get<ServerPublishReceiverFlow> ();

		readonly IConnectionProvider connectionProvider;
		readonly IPublishSenderFlow senderFlow;
		readonly IRepository<ConnectionWill> willRepository;
		readonly IPacketIdProvider packetIdProvider;
		readonly IEventStream eventStream;

		public ServerPublishReceiverFlow (ITopicEvaluator topicEvaluator,
			IConnectionProvider connectionProvider,
			IPublishSenderFlow senderFlow,
			IRepository<RetainedMessage> retainedRepository,
			IRepository<ClientSession> sessionRepository,
			IRepository<ConnectionWill> willRepository,
			IPacketIdProvider packetIdProvider,
			IEventStream eventStream,
			ProtocolConfiguration configuration)
			: base (topicEvaluator, retainedRepository, sessionRepository, configuration)
		{
			this.connectionProvider = connectionProvider;
			this.senderFlow = senderFlow;
			this.willRepository = willRepository;
			this.packetIdProvider = packetIdProvider;
			this.eventStream = eventStream;
		}

		protected override async Task ProcessPublishAsync (Publish publish, string clientId)
		{
			if (publish.Retain) {
				var existingRetainedMessage = retainedRepository.Get(r => r.Topic == publish.Topic);

				if (existingRetainedMessage != null) {
					retainedRepository.Delete (existingRetainedMessage);
				}

				if (publish.Payload.Length > 0) {
					var retainedMessage = new RetainedMessage {
						Topic = publish.Topic,
						QualityOfService = publish.QualityOfService,
						Payload = publish.Payload
					};

					retainedRepository.Create (retainedMessage);
				}
			}

			await DispatchAsync (publish, clientId)
				.ConfigureAwait (continueOnCapturedContext: false);
		}

		internal async Task SendWillAsync (string clientId)
		{
			var will = willRepository.Get (w => w.ClientId == clientId);

			if (will != null && will.Will != null) {
				var willPublish = new Publish (will.Will.Topic, will.Will.QualityOfService, will.Will.Retain, duplicated: false) {
					Payload = Encoding.UTF8.GetBytes (will.Will.Message)
				};

				tracer.Info (Props.Resources.Tracer_ServerPublishReceiverFlow_SendingWill, clientId, willPublish.Topic);

				await DispatchAsync (willPublish, clientId, isWill: true)
					.ConfigureAwait (continueOnCapturedContext: false);
			}
		}

		async Task DispatchAsync (Publish publish, string clientId, bool isWill = false)
		{
			var subscriptions = sessionRepository
				.GetAll ().ToList ()
				.SelectMany (s => s.GetSubscriptions ())
				.Where (x => topicEvaluator.Matches (publish.Topic, x.TopicFilter));

			if (!subscriptions.Any ()) {
				tracer.Verbose (Props.Resources.Tracer_ServerPublishReceiverFlow_TopicNotSubscribed, publish.Topic, clientId);

				eventStream.Push (new TopicNotSubscribed { Topic = publish.Topic, SenderId = clientId, Payload = publish.Payload });
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
			ushort? packetId = supportedQos == QualityOfService.AtMostOnce ? null : (ushort?)packetIdProvider.GetPacketId ();
			var subscriptionPublish = new Publish (publish.Topic, supportedQos, retain, duplicated: false, packetId: packetId) {
				Payload = publish.Payload
			};
			var clientChannel = connectionProvider.GetConnection (subscription.ClientId);

			await senderFlow.SendPublishAsync (subscription.ClientId, subscriptionPublish, clientChannel)
				.ConfigureAwait (continueOnCapturedContext: false);
		}
	}
}
