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
			: base(topicEvaluator, retainedRepository, sessionRepository, configuration)
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

			await this.DispatchAsync (publish, clientId)
				.ConfigureAwait(continueOnCapturedContext: false);
		}

		internal async Task SendWillAsync(string clientId)
		{
			var will = this.willRepository.Get (w => w.ClientId == clientId);

			if (will != null && will.Will != null) {
				var willPublish = new Publish (will.Will.Topic, will.Will.QualityOfService, will.Will.Retain, duplicated: false) {
					Payload = Encoding.UTF8.GetBytes (will.Will.Message)
				};

				tracer.Info (Props.Resources.Tracer_ServerPublishReceiverFlow_SendingWill, clientId, willPublish.Topic);

				await this.DispatchAsync(willPublish, clientId, isWill: true)
					.ConfigureAwait(continueOnCapturedContext: false);
			}
		}

		private async Task DispatchAsync (Publish publish, string clientId, bool isWill = false)
		{
			var subscriptions = this.sessionRepository
				.GetAll ().ToList ()
				.SelectMany (s => s.GetSubscriptions ())
				.Where (x => this.topicEvaluator.Matches (publish.Topic, x.TopicFilter));

			if (!subscriptions.Any ()) {
				tracer.Verbose (Props.Resources.Tracer_ServerPublishReceiverFlow_TopicNotSubscribed, publish.Topic, clientId);

				this.eventStream.Push (new TopicNotSubscribed { Topic = publish.Topic, SenderId = clientId, Payload = publish.Payload });
			} else {
				foreach (var subscription in subscriptions) {
					await this.DispatchAsync (publish, subscription, isWill)
						.ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}

		private async Task DispatchAsync (Publish publish, ClientSubscription subscription, bool isWill = false)
		{
			var requestedQos = isWill ? publish.QualityOfService : subscription.MaximumQualityOfService;
			var supportedQos = configuration.GetSupportedQos(requestedQos);
			var retain = isWill ? publish.Retain : false;
			ushort? packetId = supportedQos == QualityOfService.AtMostOnce ? null : (ushort?)this.packetIdProvider.GetPacketId ();
			var subscriptionPublish = new Publish (publish.Topic, supportedQos, retain, duplicated: false, packetId: packetId) {
				Payload = publish.Payload
			};
			var clientChannel = this.connectionProvider.GetConnection (subscription.ClientId);

			await this.senderFlow.SendPublishAsync (subscription.ClientId, subscriptionPublish, clientChannel)
				.ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
