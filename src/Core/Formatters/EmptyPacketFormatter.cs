using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt.Formatters
{
	public class EmptyPacketFormatter <T> : Formatter<T>
		where T : class, IPacket, new()
	{
		readonly PacketType packetType;

		public EmptyPacketFormatter (PacketType packetType)
		{
			this.packetType = packetType;
		}

		public override PacketType PacketType { get { return this.packetType; } }

		protected override T Read (byte[] bytes)
		{
			this.ValidateHeaderFlag (bytes, t => t == this.packetType, 0x00);

			return new T();
		}

		protected override byte[] Write (T packet)
		{
			var flags = 0x00;
			var type = Convert.ToInt32(this.packetType) << 4;

			var fixedHeaderByte1 = Convert.ToByte(flags | type);
			var fixedHeaderByte2 = Convert.ToByte (0x00);

			return new byte[] { fixedHeaderByte1, fixedHeaderByte2 };
		}
	}
}
