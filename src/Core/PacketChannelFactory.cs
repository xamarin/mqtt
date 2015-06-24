using System.Collections.Generic;
using System.Net.Mqtt.Formatters;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	internal class PacketChannelFactory : IPacketChannelFactory
	{
		readonly IChannelFactory innerChannelFactory;
		readonly ITopicEvaluator topicEvaluator;
		readonly ProtocolConfiguration configuration;

		public PacketChannelFactory (IChannelFactory innerChannelFactory, ITopicEvaluator topicEvaluator, ProtocolConfiguration configuration)
			: this(topicEvaluator, configuration)
		{
			this.innerChannelFactory = innerChannelFactory;
		}

		public PacketChannelFactory (ITopicEvaluator topicEvaluator, ProtocolConfiguration configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.configuration = configuration;
		}

		public IChannel<IPacket> Create ()
		{
			if (this.innerChannelFactory == null) {
				throw new ProtocolException (Properties.Resources.PacketChannelFactory_InnerChannelFactoryNotFound);
			}

			var binaryChannel = this.innerChannelFactory.Create ();

			return this.Create (binaryChannel);
		}

		public IChannel<IPacket> Create (IChannel<byte[]> binaryChannel)
		{
			var formatters = this.GetFormatters();
			var manager = new PacketManager (formatters);

			return new PacketChannel (binaryChannel, manager, this.configuration);
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
