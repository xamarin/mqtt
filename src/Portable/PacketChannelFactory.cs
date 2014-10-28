using System.Collections.Generic;
using Hermes.Formatters;
using Hermes.Packets;

namespace Hermes
{
	public class PacketChannelFactory : IPacketChannelFactory
	{
		public IChannel<IPacket> CreateChannel (IBufferedChannel<byte> socket)
		{
			var binaryChannel = new BinaryChannel (socket);

			var formatters = this.GetFormatters();
			var manager = new PacketManager (formatters);
			
			//TODO: Issue! Can't assign packet manager before it has the formatters assigned.  See PROPOSAL TODO's
			//But I can't create the formatters until I have the packet channel (circular dependency)
			return new PacketChannel (binaryChannel, manager);
		}

		private IEnumerable<IFormatter> GetFormatters(IChannel<IPacket> reader, IChannel<byte[]> writer)
		{
			var formatters = new List<IFormatter> ();
			
			formatters.Add (new ConnectFormatter (reader, writer));
			formatters.Add (new ConnectAckFormatter (reader, writer));
			formatters.Add (new PublishFormatter (reader, writer));
			formatters.Add (new FlowPacketFormatter<PublishAck>(PacketType.PublishAck, id => new PublishAck(id), reader, writer));
			formatters.Add (new FlowPacketFormatter<PublishReceived>(PacketType.PublishReceived, id => new PublishReceived(id), reader, writer));
			formatters.Add (new FlowPacketFormatter<PublishRelease>(PacketType.PublishRelease, id => new PublishRelease(id), reader, writer));
			formatters.Add (new FlowPacketFormatter<PublishComplete>(PacketType.PublishComplete, id => new PublishComplete(id), reader, writer));
			formatters.Add (new SubscribeFormatter (reader, writer));
			formatters.Add (new SubscribeAckFormatter (reader, writer));
			formatters.Add (new UnsubscribeFormatter (reader, writer));
			formatters.Add (new FlowPacketFormatter<UnsubscribeAck> (PacketType.UnsubscribeAck, id => new UnsubscribeAck(id), reader, writer));
			formatters.Add (new EmptyPacketFormatter<PingRequest> (PacketType.PingRequest, reader, writer));
			formatters.Add (new EmptyPacketFormatter<PingResponse> (PacketType.PingResponse, reader, writer));
			formatters.Add (new EmptyPacketFormatter<Disconnect> (PacketType.Disconnect, reader, writer));

			return formatters;
		}
	}
}
