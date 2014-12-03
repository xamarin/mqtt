using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class ServerPublishReceiverFlow : PublishReceiverFlow
	{
		public ServerPublishReceiverFlow (ITopicEvaluator topicEvaluator,
			IRepository<RetainedMessage> retainedRepository, 
			IRepository<ClientSession> sessionRepository,
			IRepository<PacketIdentifier> packetIdentifierRepository,
			IPublishDispatcher publishDispatcher,
			ProtocolConfiguration configuration)
			: base(topicEvaluator, retainedRepository, sessionRepository, packetIdentifierRepository, configuration)
		{
			this.PublishDispatcher = publishDispatcher;
		}

		public IPublishDispatcher PublishDispatcher { get; private set; }

		protected override async Task ProcessPublishAsync (Publish publish)
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

			await this.PublishDispatcher.DispatchAsync (publish);
		}
	}
}
