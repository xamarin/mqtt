using System;
using System.Threading.Tasks;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes.Formatters
{
	public abstract class Formatter<T> : IFormatter
		where T : class, IPacket
	{
		public abstract PacketType PacketType { get; }

		protected abstract T Read (byte[] bytes);

		protected abstract byte[] Write (T packet);

		/// <exception cref="ConnectProtocolException">ConnectProtocolException</exception>
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task<IPacket> FormatAsync (byte[] bytes)
		{
			var actualType = (PacketType)bytes.Byte (0).Bits (4);

			if (PacketType != actualType) {
				var error = string.Format(Resources.Formatter_InvalidPacket, typeof(T).Name);

				throw new ProtocolException (error);
			}

			var packet = await Task.Run(() => this.Read (bytes));

			return packet;
		}

		/// <exception cref="ConnectProtocolException">ConnectProtocolException</exception>
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task<byte[]> FormatAsync (IPacket packet)
		{
			if (packet.Type != PacketType) {
				var error = string.Format(Resources.Formatter_InvalidPacket, typeof(T).Name);

				throw new ProtocolException (error);
			}

			var bytes = await Task.Run(() => this.Write (packet as T));

			return bytes;
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
