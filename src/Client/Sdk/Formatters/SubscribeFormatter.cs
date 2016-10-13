using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk.Formatters
{
	internal class SubscribeFormatter : Formatter<Subscribe>
	{
		readonly IMqttTopicEvaluator topicEvaluator;

		public SubscribeFormatter (IMqttTopicEvaluator topicEvaluator)
		{
			this.topicEvaluator = topicEvaluator;
		}

		public override MqttPacketType PacketType { get { return Packets.MqttPacketType.Subscribe; } }

		protected override Subscribe Read (byte[] bytes)
		{
			ValidateHeaderFlag (bytes, t => t == MqttPacketType.Subscribe, 0x02);

			var remainingLengthBytesLength = 0;
			var remainingLength = MqttProtocol.Encoding.DecodeRemainingLength (bytes, out remainingLengthBytesLength);

			var packetIdentifierStartIndex = remainingLengthBytesLength + 1;
			var packetIdentifier = bytes.Bytes (packetIdentifierStartIndex, 2).ToUInt16();

			var headerLength = 1 + remainingLengthBytesLength + 2;
			var subscriptions = GetSubscriptions(bytes, headerLength, remainingLength);

			return new Subscribe (packetIdentifier, subscriptions.ToArray ());
		}

		protected override byte[] Write (Subscribe packet)
		{
			var bytes = new List<byte> ();

			var variableHeader = GetVariableHeader (packet);
			var payload = GetPayload (packet);
			var remainingLength = MqttProtocol.Encoding.EncodeRemainingLength (variableHeader.Length + payload.Length);
			var fixedHeader = GetFixedHeader (remainingLength);

			bytes.AddRange (fixedHeader);
			bytes.AddRange (variableHeader);
			bytes.AddRange (payload);

			return bytes.ToArray ();
		}

		byte[] GetFixedHeader (byte[] remainingLength)
		{
			var fixedHeader = new List<byte> ();

			var flags = 0x02;
			var type = Convert.ToInt32(MqttPacketType.Subscribe) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);

			fixedHeader.Add (fixedHeaderByte1);
			fixedHeader.AddRange (remainingLength);

			return fixedHeader.ToArray ();
		}

		byte[] GetVariableHeader (Subscribe packet)
		{
			var variableHeader = new List<byte> ();

			var packetIdBytes = MqttProtocol.Encoding.EncodeInteger(packet.PacketId);

			variableHeader.AddRange (packetIdBytes);

			return variableHeader.ToArray ();
		}

		byte[] GetPayload (Subscribe packet)
		{
			if (packet.Subscriptions == null || !packet.Subscriptions.Any ())
				throw new MqttProtocolViolationException  (Properties.Resources.SubscribeFormatter_MissingTopicFilterQosPair);

			var payload = new List<byte> ();

			foreach (var subscription in packet.Subscriptions) {
                if (string.IsNullOrEmpty (subscription.TopicFilter)) {
                    throw new MqttProtocolViolationException (Properties.Resources.SubscribeFormatter_MissingTopicFilterQosPair);
                }

				if (!topicEvaluator.IsValidTopicFilter (subscription.TopicFilter)) {
					var error = string.Format  (Properties.Resources.SubscribeFormatter_InvalidTopicFilter, subscription.TopicFilter);

					throw new MqttException (error);
				}

				var topicBytes = MqttProtocol.Encoding.EncodeString (subscription.TopicFilter);
				var requestedQosByte = Convert.ToByte (subscription.MaximumQualityOfService);

				payload.AddRange (topicBytes);
				payload.Add (requestedQosByte);
			}

			return payload.ToArray ();
		}

		IEnumerable<Subscription> GetSubscriptions (byte[] bytes, int headerLength, int remainingLength)
		{
			if (bytes.Length - headerLength < 4) //At least 4 bytes required on payload: MSB, LSB, Topic Filter, Requests QoS
				throw new MqttProtocolViolationException  (Properties.Resources.SubscribeFormatter_MissingTopicFilterQosPair);

			var index = headerLength;

			do {
				var topicFilter = bytes.GetString (index, out index);

				if (!topicEvaluator.IsValidTopicFilter (topicFilter)) {
					var error = string.Format  (Properties.Resources.SubscribeFormatter_InvalidTopicFilter, topicFilter);

					throw new MqttException (error);
				}

				var requestedQosByte = bytes.Byte (index);

				if (!Enum.IsDefined (typeof (MqttQualityOfService), requestedQosByte))
					throw new MqttProtocolViolationException  (Properties.Resources.Formatter_InvalidQualityOfService);

				var requestedQos = (MqttQualityOfService)requestedQosByte;

				yield return new Subscription (topicFilter, requestedQos);
				index++;
			} while (bytes.Length - index + 1 >= 2);
		}
	}
}
