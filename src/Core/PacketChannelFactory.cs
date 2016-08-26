using System.Collections.Generic;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Formatters;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	internal class PacketChannelFactory : IPacketChannelFactory
	{
		readonly IChannelFactory innerChannelFactory;
		readonly ITopicEvaluator topicEvaluator;
		readonly ITracerManager tracerManager;
		readonly ProtocolConfiguration configuration;

		public PacketChannelFactory (IChannelFactory innerChannelFactory, 
			ITopicEvaluator topicEvaluator, 
			ITracerManager tracerManager,
			ProtocolConfiguration configuration)
			: this (topicEvaluator, tracerManager, configuration)
		{
			this.innerChannelFactory = innerChannelFactory;
		}

		public PacketChannelFactory (ITopicEvaluator topicEvaluator,
			ITracerManager tracerManager,
			ProtocolConfiguration configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.tracerManager = tracerManager;
			this.configuration = configuration;
		}

		public IChannel<IPacket> Create ()
		{
			if (innerChannelFactory == null) {
				throw new MqttException (Properties.Resources.PacketChannelFactory_InnerChannelFactoryNotFound);
			}

			var binaryChannel = innerChannelFactory.Create ();

			return Create (binaryChannel);
		}

		public IChannel<IPacket> Create (IChannel<byte[]> binaryChannel)
		{
			var formatters = GetFormatters();
			var packetManager = new PacketManager (formatters);

			return new PacketChannel (binaryChannel, packetManager, tracerManager, configuration);
		}

		IEnumerable<IFormatter> GetFormatters ()
		{
			var formatters = new List<IFormatter> ();

			formatters.Add (new ConnectFormatter ());
			formatters.Add (new ConnectAckFormatter ());
			formatters.Add (new PublishFormatter (topicEvaluator));
			formatters.Add (new FlowPacketFormatter<PublishAck> (PacketType.PublishAck, id => new PublishAck (id)));
			formatters.Add (new FlowPacketFormatter<PublishReceived> (PacketType.PublishReceived, id => new PublishReceived (id)));
			formatters.Add (new FlowPacketFormatter<PublishRelease> (PacketType.PublishRelease, id => new PublishRelease (id)));
			formatters.Add (new FlowPacketFormatter<PublishComplete> (PacketType.PublishComplete, id => new PublishComplete (id)));
			formatters.Add (new SubscribeFormatter (topicEvaluator));
			formatters.Add (new SubscribeAckFormatter ());
			formatters.Add (new UnsubscribeFormatter ());
			formatters.Add (new FlowPacketFormatter<UnsubscribeAck> (PacketType.UnsubscribeAck, id => new UnsubscribeAck (id)));
			formatters.Add (new EmptyPacketFormatter<PingRequest> (PacketType.PingRequest));
			formatters.Add (new EmptyPacketFormatter<PingResponse> (PacketType.PingResponse));
			formatters.Add (new EmptyPacketFormatter<Disconnect> (PacketType.Disconnect));

			return formatters;
		}
	}
}
