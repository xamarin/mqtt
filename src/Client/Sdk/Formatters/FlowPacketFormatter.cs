using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk.Formatters
{
	internal class FlowPacketFormatter<T> : Formatter<T>
		where T : class, IFlowPacket
	{
		readonly MqttPacketType packetType;
		readonly Func<ushort, T> packetFactory;

		public FlowPacketFormatter (MqttPacketType packetType, Func<ushort, T> packetFactory)
		{
			this.packetType = packetType;
			this.packetFactory = packetFactory;
		}

		public override MqttPacketType PacketType { get { return packetType; } }

		protected override T Read (byte[] bytes)
		{
			ValidateHeaderFlag (bytes, t => t == MqttPacketType.PublishRelease, 0x02);
			ValidateHeaderFlag (bytes, t => t != MqttPacketType.PublishRelease, 0x00);

			var remainingLengthBytesLength = 0;

			MqttProtocol.Encoding.DecodeRemainingLength (bytes, out remainingLengthBytesLength);

			var packetIdIndex = MqttProtocol.PacketTypeLength + remainingLengthBytesLength;
			var packetIdBytes = bytes.Bytes (packetIdIndex, 2);

			return packetFactory (packetIdBytes.ToUInt16 ());
		}

		protected override byte[] Write (T packet)
		{
			var variableHeader = MqttProtocol.Encoding.EncodeInteger(packet.PacketId);
			var remainingLength = MqttProtocol.Encoding.EncodeRemainingLength (variableHeader.Length);
			var fixedHeader = GetFixedHeader (packet.Type, remainingLength);
			var bytes = new byte[fixedHeader.Length + variableHeader.Length];

			fixedHeader.CopyTo (bytes, 0);
			variableHeader.CopyTo (bytes, fixedHeader.Length);

			return bytes;
		}

		byte[] GetFixedHeader (MqttPacketType packetType, byte[] remainingLength)
		{
			// MQTT 2.2.2: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/csprd02/mqtt-v3.1.1-csprd02.html#_Toc385349758
			// The flags for PUBREL are different than for the other flow packets.
			var flags = packetType == Packets.MqttPacketType.PublishRelease ? 0x02 : 0x00;
			var type = Convert.ToInt32(packetType) << 4;
			var fixedHeaderByte1 = Convert.ToByte(flags | type);
			var fixedHeader = new byte[1 + remainingLength.Length];

			fixedHeader[0] = fixedHeaderByte1;
			remainingLength.CopyTo (fixedHeader, 1);

			return fixedHeader;
		}
	}
}
