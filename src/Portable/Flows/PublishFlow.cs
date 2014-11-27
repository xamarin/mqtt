using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Storage;

namespace Hermes.Flows
{
	public abstract class PublishFlow : IPublishFlow
	{
		protected readonly IClientManager clientManager;
		protected readonly IRepository<ClientSession> sessionRepository;
		protected readonly ProtocolConfiguration configuration;

		protected PublishFlow (IClientManager clientManager, 
			IRepository<ClientSession> sessionRepository, 
			ProtocolConfiguration configuration)
		{
			this.clientManager = clientManager;
			this.sessionRepository = sessionRepository;
			this.configuration = configuration;
		}

		public abstract Task ExecuteAsync (string clientId, IPacket input, IChannel<IPacket> channel);

		public async Task SendAckAsync (string clientId, IFlowPacket ack, IChannel<IPacket> channel)
		{
			if(ack.Type == PacketType.PublishReceived || ack.Type == PacketType.PublishRelease)
				this.StorePendingAcknowledgement (ack, clientId);

			if(ack.Type == PacketType.PublishReceived)
				this.MonitorAck<PublishRelease> (ack, channel);
			else if (ack.Type == PacketType.PublishRelease)
				this.MonitorAck<PublishComplete> (ack, channel);

			await channel.SendAsync (ack);
		}

		protected void MonitorAck<T>(IFlowPacket sentPacket, IChannel<IPacket> channel)
			where T : IFlowPacket
		{
			channel.Receiver
				.OfType<T> ()
				.FirstAsync (ack => ack.PacketId == sentPacket.PacketId)
				.Timeout (new TimeSpan (0, 0, this.configuration.WaitingTimeoutSecs))
				.Subscribe (_ => { }, async ex => {
					await channel.SendAsync (sentPacket);
				});
		}

		private void StorePendingAcknowledgement(IFlowPacket ack, string clientId)
		{
			if (ack.Type != PacketType.PublishReceived && ack.Type != PacketType.PublishRelease)
				return;

			var unacknowledgeMessage = new PendingAcknowledgement {
				PacketId = ack.PacketId,
				Type = ack.Type
			};

			var session = this.sessionRepository.Get (s => s.ClientId == clientId);

			session.PendingAcknowledgements.Add (unacknowledgeMessage);

			this.sessionRepository.Update (session);
		}
	}
}
