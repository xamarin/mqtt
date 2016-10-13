using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk.Formatters
{
	internal class EmptyPacketFormatter<T> : Formatter<T>
		where T : class, IPacket, new()
	{
		readonly MqttPacketType packetType;

		public EmptyPacketFormatter (MqttPacketType packetType)
		{
			this.packetType = packetType;
		}

		public override MqttPacketType PacketType { get { return packetType; } }

		protected override T Read (byte[] bytes)
		{
			ValidateHeaderFlag (bytes, t => t == packetType, 0x00);

			return new T ();
		}

		protected override byte[] Write (T packet)
		{
			var flags = 0x00;
			var type = Convert.ToInt32(packetType) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);
			var fixedHeaderByte2 = Convert.ToByte (0x00);

			return new byte[] { fixedHeaderByte1, fixedHeaderByte2 };
		}
	}
}
