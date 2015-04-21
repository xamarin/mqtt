using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hermes.Diagnostics;
using Hermes.Packets;
using Hermes.Properties;
using Hermes.Storage;

namespace Hermes.Flows
{
	public class PublishSenderFlow : PublishFlow, IPublishSenderFlow
	{
		private static readonly ITracer tracer = Tracer.Get<PublishSenderFlow> ();

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

			if (!this.senderRules.TryGetValue (input.Type, out senderRule)) {
				return;
			}

			var flowPacket = input as IFlowPacket;

			if (flowPacket == null) {
				return;
			}

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

			if(qos == QualityOfService.AtLeastOnce) {
				await this.MonitorAck<PublishAck> (message, clientId, channel);
			} else if (qos == QualityOfService.ExactlyOnce) {
				await this.MonitorAck<PublishReceived> (message, clientId, channel);
			}
		}

		protected void RemovePendingMessage(string clientId, ushort packetId)
		{
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new ProtocolException (string.Format(Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			var pendingMessage = session.PendingMessages.FirstOrDefault(p => p.PacketId.HasValue 
				&& p.PacketId.Value == packetId);

			session.PendingMessages.Remove (pendingMessage);

			this.sessionRepository.Update (session);
		}

		protected async Task MonitorAck<T>(Publish sentMessage, string clientId, IChannel<IPacket> channel)
			where T : IFlowPacket
		{
			await this.GetAckMonitor<T> (sentMessage, clientId, channel);
		}

		protected IObservable<T> GetAckMonitor<T>(Publish sentMessage, string clientId, IChannel<IPacket> channel, int retries = 0)
			where T : IFlowPacket
		{
			if (retries == this.configuration.QualityOfServiceAckRetries) {
				throw new ProtocolException (string.Format(Resources.PublishFlow_AckMonitor_ExceededMaximumAckRetries, this.configuration.QualityOfServiceAckRetries));
			}

			return channel.Receiver.OfType<T> ()
				.FirstOrDefaultAsync (x => x.PacketId == sentMessage.PacketId.Value)
				.Timeout (TimeSpan.FromSeconds (this.configuration.WaitingTimeoutSecs))
				.SubscribeOn(NewThreadScheduler.Default)
				.Catch<T, TimeoutException> (timeEx => {
					tracer.Warn (timeEx, Resources.Tracer_PublishFlow_RetryingQoSFlow, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff"), sentMessage.Type, clientId);

					var duplicated = new Publish (sentMessage.Topic, sentMessage.QualityOfService,
						sentMessage.Retain, duplicated: true, packetId: sentMessage.PacketId);

					channel.SendAsync (duplicated).Wait();

					return this.GetAckMonitor<T> (sentMessage, clientId, channel, retries + 1);
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
			if (message.QualityOfService == QualityOfService.AtMostOnce) {
				return;
			}

			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new ProtocolException (string.Format(Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			var savedMessage = new PendingMessage {
				Status = status,
				QualityOfService = message.QualityOfService,
				Duplicated = message.Duplicated,
				Retain = message.Retain,
				Topic = message.Topic,
				PacketId = message.PacketId,
				Payload = message.Payload
			};

			session.PendingMessages.Add (savedMessage);

			this.sessionRepository.Update (session);
		}
	}
}
