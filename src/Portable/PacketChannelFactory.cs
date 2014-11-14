using System.Collections.Generic;
using Hermes.Formatters;
using Hermes.Packets;

namespace Hermes
{
	public class PacketChannelFactory : IPacketChannelFactory
	{
		readonly ITopicEvaluator topicEvaluator;

		public PacketChannelFactory (ITopicEvaluator topicEvaluator)
		{
			this.topicEvaluator = topicEvaluator;
		}

		public IChannel<IPacket> CreateChannel (IBufferedChannel<byte> socket)
		{
			var binaryChannel = new BinaryChannel (socket);

			var formatters = this.GetFormatters();
			var manager = new PacketManager (formatters);
			
			return new PacketChannel (binaryChannel, manager);
		}

		private IEnumerable<IFormatter> GetFormatters()
		{
			var formatters = new List<IFormatter> ();
			
			formatters.Add (new ConnectFormatter ());
			formatters.Add (new ConnectAckFormatter ());
			formatters.Add (new PublishFormatter (this.topicEvaluator));
			formatters.Add (new FlowPacketFormatter<PublishAck>(PacketType.PublishAck, id => new PublishAck(id)));
			formatters.Add (new FlowPacketFormatter<PublishReceived>(PacketType.PublishReceived, id => new PublishReceived(id)));
			formatters.Add (new FlowPacketFormatter<PublishRelease>(PacketType.PublishRelease, id => new PublishRelease(id)));
			formatters.Add (new FlowPacketFormatter<PublishComplete>(PacketType.PublishComplete, id => new PublishComplete(id)));
			formatters.Add (new SubscribeFormatter (this.topicEvaluator));
			formatters.Add (new SubscribeAckFormatter ());
			formatters.Add (new UnsubscribeFormatter ());
			formatters.Add (new FlowPacketFormatter<UnsubscribeAck> (PacketType.UnsubscribeAck, id => new UnsubscribeAck(id)));
			formatters.Add (new EmptyPacketFormatter<PingRequest> (PacketType.PingRequest));
			formatters.Add (new EmptyPacketFormatter<PingResponse> (PacketType.PingResponse));
			formatters.Add (new EmptyPacketFormatter<Disconnect> (PacketType.Disconnect));

			return formatters;
		}
	}
}
