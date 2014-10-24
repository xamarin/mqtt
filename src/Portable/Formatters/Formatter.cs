using System;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes.Formatters
{
	public abstract class Formatter<T> : IFormatter
		where T : class, IPacket
	{
		readonly IChannel<IPacket> reader;
		readonly IChannel<byte[]> writer;

		public Formatter (IChannel<IPacket> reader, IChannel<byte[]> writer)
		{
			this.reader = reader;
			this.writer = writer;
		}
		
		public abstract PacketType PacketType { get; }

		protected abstract T Read (byte[] bytes);

		protected abstract byte[] Write (T packet);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task ReadAsync (byte[] bytes)
		{
			var actualType = (PacketType)bytes.Byte (0).Bits (4);

			if (PacketType != actualType) {
				var error = string.Format(Resources.Formatter_InvalidPacket, typeof(T).Name);

				throw new ProtocolException (error);
			}

			var packet = this.Read (bytes);

			await this.reader.SendAsync (packet);
		}

		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task WriteAsync (IPacket packet)
		{
			if (packet.Type != PacketType) {
				var error = string.Format(Resources.Formatter_InvalidPacket, typeof(T).Name);

				throw new ProtocolException (error);
			}

			var bytes = this.Write (packet as T);

			await this.writer.SendAsync (bytes);
		}

		protected void ValidateHeaderFlag (byte[] bytes, Func<PacketType, bool> packetTypePredicate, int expectedFlag)
		{
			var headerFlag = bytes.Byte (0).Bits (5, 4);

			if (packetTypePredicate(this.PacketType) && headerFlag != expectedFlag) {
				var error = string.Format (Resources.Formatter_InvalidHeaderFlag, headerFlag, typeof(T).Name, expectedFlag);

				throw new ProtocolException (error);
			}
		}
	}
}
