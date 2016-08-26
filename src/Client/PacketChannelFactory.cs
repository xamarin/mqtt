using System.Collections.Generic;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Formatters;
using System.Net.Mqtt.Packets;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	internal class PacketChannelFactory : IPacketChannelFactory
	{
		readonly IMqttChannelFactory innerChannelFactory;
		readonly IMqttTopicEvaluator topicEvaluator;
		readonly ITracerManager tracerManager;
		readonly MqttConfiguration configuration;

		public PacketChannelFactory (IMqttChannelFactory innerChannelFactory, 
			IMqttTopicEvaluator topicEvaluator, 
			ITracerManager tracerManager,
			MqttConfiguration configuration)
			: this (topicEvaluator, tracerManager, configuration)
		{
			this.innerChannelFactory = innerChannelFactory;
		}

		public PacketChannelFactory (IMqttTopicEvaluator topicEvaluator,
			ITracerManager tracerManager,
			MqttConfiguration configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.tracerManager = tracerManager;
			this.configuration = configuration;
		}

		public async Task<IMqttChannel<IPacket>> CreateAsync ()
		{
			if (innerChannelFactory == null) {
				throw new MqttException (Resources.PacketChannelFactory_InnerChannelFactoryNotFound);
			}

			var binaryChannel = await innerChannelFactory
                .CreateAsync ()
                .ConfigureAwait (continueOnCapturedContext: false);

			return Create (binaryChannel);
		}

		public IMqttChannel<IPacket> Create (IMqttChannel<byte[]> binaryChannel)
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
			formatters.Add (new FlowPacketFormatter<PublishAck> (MqttPacketType.PublishAck, id => new PublishAck (id)));
			formatters.Add (new FlowPacketFormatter<PublishReceived> (MqttPacketType.PublishReceived, id => new PublishReceived (id)));
			formatters.Add (new FlowPacketFormatter<PublishRelease> (MqttPacketType.PublishRelease, id => new PublishRelease (id)));
			formatters.Add (new FlowPacketFormatter<PublishComplete> (MqttPacketType.PublishComplete, id => new PublishComplete (id)));
			formatters.Add (new SubscribeFormatter (topicEvaluator));
			formatters.Add (new SubscribeAckFormatter ());
			formatters.Add (new UnsubscribeFormatter ());
			formatters.Add (new FlowPacketFormatter<UnsubscribeAck> (MqttPacketType.UnsubscribeAck, id => new UnsubscribeAck (id)));
			formatters.Add (new EmptyPacketFormatter<PingRequest> (MqttPacketType.PingRequest));
			formatters.Add (new EmptyPacketFormatter<PingResponse> (MqttPacketType.PingResponse));
			formatters.Add (new EmptyPacketFormatter<Disconnect> (MqttPacketType.Disconnect));

			return formatters;
		}
	}
}
