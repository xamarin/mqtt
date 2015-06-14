using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Storage;
using System.Threading;

namespace System.Net.Mqtt.Flows
{
	public class PublishSenderFlow : PublishFlow, IPublishSenderFlow
	{
		private static readonly ITracer tracer = Tracer.Get<PublishSenderFlow> ();

		IDictionary<PacketType, Func<string, ushort, IFlowPacket>> senderRules;

		public PublishSenderFlow (IRepository<ClientSession> sessionRepository,
			ProtocolConfiguration configuration)
			: base(sessionRepository, configuration)
		{
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
				await this.SendAckAsync (clientId, ackPacket, channel)
					.ConfigureAwait(continueOnCapturedContext: false);
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

			await channel.SendAsync (message)
				.ConfigureAwait(continueOnCapturedContext: false);

			if(qos == QualityOfService.AtLeastOnce) {
				await this.MonitorAckAsync<PublishAck> (message, clientId, channel)
					.ConfigureAwait(continueOnCapturedContext: false);
			} else if (qos == QualityOfService.ExactlyOnce) {
				await this.MonitorAckAsync<PublishReceived> (message, clientId, channel).ConfigureAwait(continueOnCapturedContext: false);
				await channel.Receiver
					.ObserveOn (NewThreadScheduler.Default)
					.OfType<PublishComplete> ()
					.FirstOrDefaultAsync (x => x.PacketId == message.PacketId.Value);
			}
		}

		protected void RemovePendingMessage(string clientId, ushort packetId)
		{
			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			if (session == null) {
				throw new ProtocolException (string.Format(Properties.Resources.SessionRepository_ClientSessionNotFound, clientId));
			}

			var pendingMessage = session
				.GetPendingMessages()
				.FirstOrDefault(p => p.PacketId.HasValue && p.PacketId.Value == packetId);

			session.RemovePendingMessage (pendingMessage);

			this.sessionRepository.Update (session);
		}

		protected async Task MonitorAckAsync<T>(Publish sentMessage, string clientId, IChannel<IPacket> channel)
			where T : IFlowPacket
		{
			var intervalSubscription = Observable
				.Interval (TimeSpan.FromSeconds (this.configuration.WaitingTimeoutSecs), NewThreadScheduler.Default)
				.Subscribe (async _ => {
					if (channel.IsConnected) {
						tracer.Warn (Resources.Tracer_PublishFlow_RetryingQoSFlow, sentMessage.Type, clientId);

						var duplicated = new Publish (sentMessage.Topic, sentMessage.QualityOfService,
							sentMessage.Retain, duplicated: true, packetId: sentMessage.PacketId) {
								Payload = sentMessage.Payload
							};

						await channel.SendAsync (duplicated);
					}
				});
			
			await channel.Receiver
				.ObserveOn (NewThreadScheduler.Default)
				.OfType<T> ()
				.FirstOrDefaultAsync (x => x.PacketId == sentMessage.PacketId.Value);

			intervalSubscription.Dispose ();
		}

		private void DefineSenderRules ()
		{
			this.senderRules = new Dictionary<PacketType, Func<string, ushort, IFlowPacket>> ();

			this.senderRules.Add (PacketType.PublishAck, (clientId, packetId) => {
				this.RemovePendingMessage (clientId, packetId);

				return default (IFlowPacket);
			});

			this.senderRules.Add (PacketType.PublishReceived, (clientId, packetId) => {
				this.RemovePendingMessage (clientId, packetId);

				return new PublishRelease(packetId);
			});

			this.senderRules.Add (PacketType.PublishComplete, (clientId, packetId) => {
				this.RemovePendingAcknowledgement (clientId, packetId, PacketType.PublishRelease);

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
				throw new ProtocolException (string.Format(Properties.Resources.SessionRepository_ClientSessionNotFound, clientId));
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

			session.AddPendingMessage (savedMessage);

			this.sessionRepository.Update (session);
		}
	}
}
