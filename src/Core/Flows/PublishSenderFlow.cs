using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class PublishSenderFlow : PublishFlow, IPublishSenderFlow
	{
		readonly IRepository<PacketIdentifier> packetIdentifierRepository;

		IDictionary<PacketType, Func<string, ushort, IFlowPacket>> senderRules;

		public PublishSenderFlow (IRepository<ClientSession> sessionRepository,
			IRepository<PacketIdentifier> packetIdentifierRepository,
			ProtocolConfiguration configuration)
			: base(sessionRepository, configuration)
		{
			this.packetIdentifierRepository = packetIdentifierRepository;

			this.DefineSenderRules ();
		}

		public override async Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel)
		{
			var senderRule = default (Func<string, ushort, IFlowPacket>);

			if (!this.senderRules.TryGetValue (input.Type, out senderRule))
				return;

			var flowPacket = input as IFlowPacket;

			if (flowPacket == null)
				return;

			var ackPacket = senderRule (clientId, flowPacket.PacketId);

			if (ackPacket != default(IFlowPacket)) {
				await this.SendAckAsync (clientId, ackPacket, channel);
			}
		}

		public async Task SendPublishAsync (string clientId, Publish message, IChannel<IPacket> channel, PendingMessageStatus status = PendingMessageStatus.PendingToSend)
		{
			if (channel == null || !channel.IsConnected) {
				this.SaveMessage (message, clientId, PendingMessageStatus.PendingToSend);
				return;
			}

			var qos = this.configuration.GetSupportedQos(message.QualityOfService);

			if (qos != QualityOfService.AtMostOnce && status == PendingMessageStatus.PendingToSend) {
				this.SaveMessage (message, clientId, PendingMessageStatus.PendingToAcknowledge);
			}

			await channel.SendAsync (message);

			if(qos == QualityOfService.AtLeastOnce)
				await this.MonitorAck<PublishAck> (message, channel);
			else if (qos == QualityOfService.ExactlyOnce)
				await this.MonitorAck<PublishReceived> (message, channel);
		}

		protected void RemovePendingMessage(string clientId, ushort packetId)
		{
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);
			var pendingMessage = session.PendingMessages.FirstOrDefault(p => p.PacketId.HasValue 
				&& p.PacketId.Value == packetId);

			session.PendingMessages.Remove (pendingMessage);

			this.sessionRepository.Update (session);
		}

		protected async Task MonitorAck<T>(Publish sentMessage, IChannel<IPacket> channel)
			where T : IFlowPacket
		{
			await this.GetAckMonitor<T> (sentMessage, channel);
		}

		protected IObservable<T> GetAckMonitor<T>(Publish sentMessage, IChannel<IPacket> channel, int retries = 0)
			where T : IFlowPacket
		{
			if (retries == this.configuration.QualityOfServiceAckRetries) {
				throw new ProtocolException (string.Format(Resources.PublishFlow_AckMonitor_ExceededMaximumAckRetries, this.configuration.QualityOfServiceAckRetries));
			}

			return channel.Receiver.OfType<T> ()
				.FirstOrDefaultAsync (x => x.PacketId == sentMessage.PacketId.Value)
				.Timeout (TimeSpan.FromSeconds (this.configuration.WaitingTimeoutSecs))
				.Catch<T, TimeoutException> (timeEx => {
					var duplicated = new Publish (sentMessage.Topic, sentMessage.QualityOfService,
						sentMessage.Retain, duplicated: true, packetId: sentMessage.PacketId);

					channel.SendAsync (duplicated).Wait();

					return this.GetAckMonitor<T> (sentMessage, channel, retries + 1);
				});
		}

		private void DefineSenderRules ()
		{
			this.senderRules = new Dictionary<PacketType, Func<string, ushort, IFlowPacket>> ();

			this.senderRules.Add (PacketType.PublishAck, (clientId, packetId) => {
				this.RemovePendingMessage (clientId, packetId);

				this.packetIdentifierRepository.Delete (i => i.Value == packetId);

				return default (IFlowPacket);
			});

			this.senderRules.Add (PacketType.PublishReceived, (clientId, packetId) => {
				this.RemovePendingMessage (clientId, packetId);

				return new PublishRelease(packetId);
			});

			this.senderRules.Add (PacketType.PublishComplete, (clientId, packetId) => {
				this.RemovePendingAcknowledgement (clientId, packetId, PacketType.PublishRelease);

				this.packetIdentifierRepository.Delete (i => i.Value == packetId);

				return default (IFlowPacket);
			});
		}

		private void SaveMessage(Publish message, string clientId, PendingMessageStatus status)
		{
			if (message.QualityOfService == QualityOfService.AtMostOnce)
				return;

			var savedMessage = new PendingMessage {
				Status = status,
				QualityOfService = message.QualityOfService,
				Duplicated = message.Duplicated,
				Retain = message.Retain,
				Topic = message.Topic,
				PacketId = message.PacketId,
				Payload = message.Payload
			};
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			session.PendingMessages.Add (savedMessage);

			this.sessionRepository.Update (session);
		}
	}
}
