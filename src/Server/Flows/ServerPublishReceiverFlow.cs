using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Hermes.Diagnostics;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerPublishReceiverFlow : PublishReceiverFlow
	{
		static readonly ITracer tracer = Tracer.Get<ServerPublishReceiverFlow> ();

		readonly IConnectionProvider connectionProvider;
		readonly IPublishSenderFlow senderFlow;
		readonly IPacketIdProvider packetIdProvider;
		readonly IEventStream eventStream;

		public ServerPublishReceiverFlow (ITopicEvaluator topicEvaluator,
			IConnectionProvider connectionProvider,
			IPublishSenderFlow senderFlow,
			IRepository<RetainedMessage> retainedRepository, 
			IRepository<ClientSession> sessionRepository,
			IPacketIdProvider packetIdProvider,
			IEventStream eventStream,
			ProtocolConfiguration configuration)
			: base(topicEvaluator, retainedRepository, sessionRepository, configuration)
		{
			this.connectionProvider = connectionProvider;
			this.senderFlow = senderFlow;
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

		private async Task DispatchAsync (Publish publish, string clientId)
		{
			var sessions = this.sessionRepository.GetAll ();
			var subscriptions = sessions.SelectMany(s => s.Subscriptions)
				.Where(x => this.topicEvaluator.Matches(publish.Topic, x.TopicFilter));

			if (!subscriptions.Any ()) {
				tracer.Verbose (Resources.Tracer_ServerPublishReceiverFlow_TopicNotSubscribed, publish.Topic, clientId);

				this.eventStream.Push (new TopicNotSubscribed { Topic = publish.Topic, SenderId = clientId, Payload = publish.Payload });
			} else {
				foreach (var subscription in subscriptions) {
					await this.DispatchAsync (subscription, publish)
						.ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}

		private async Task DispatchAsync (ClientSubscription subscription, Publish publish)
		{
			var requestedQos = configuration.GetSupportedQos(subscription.MaximumQualityOfService);
			ushort? packetId = requestedQos == QualityOfService.AtMostOnce ? null : (ushort?)this.packetIdProvider.GetPacketId ();
			var subscriptionPublish = new Publish (publish.Topic, requestedQos, retain: false, duplicated: false, packetId: packetId) {
				Payload = publish.Payload
			};
			var clientChannel = this.connectionProvider.GetConnection (subscription.ClientId);

			await this.senderFlow.SendPublishAsync (subscription.ClientId, subscriptionPublish, clientChannel)
				.ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
