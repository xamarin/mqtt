using System.Collections.Generic;
using System.Net.Mqtt.Sdk.Formatters;
using System.Net.Mqtt.Sdk.Packets;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk
{
    internal class PacketChannelFactory : IPacketChannelFactory
	{
		readonly IMqttChannelFactory innerChannelFactory;
		readonly IMqttTopicEvaluator topicEvaluator;
		readonly MqttConfiguration configuration;

		public PacketChannelFactory (IMqttChannelFactory innerChannelFactory, 
			IMqttTopicEvaluator topicEvaluator, 
			MqttConfiguration configuration)
			: this (topicEvaluator, configuration)
		{
			this.innerChannelFactory = innerChannelFactory;
		}

		public PacketChannelFactory (IMqttTopicEvaluator topicEvaluator,
			MqttConfiguration configuration)
		{
			this.topicEvaluator = topicEvaluator;
			this.configuration = configuration;
		}

		public async Task<IMqttChannel<IPacket>> CreateAsync ()
		{
			if (innerChannelFactory == null) {
				throw new MqttException (Properties.Resources.PacketChannelFactory_InnerChannelFactoryNotFound);
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

			return new PacketChannel (binaryChannel, packetManager, configuration);
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
