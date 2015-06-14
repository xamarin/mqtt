using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mqtt.Formatters;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	public class PacketManager : IPacketManager
	{
		readonly IDictionary<PacketType, IFormatter> formatters;

		public PacketManager (params IFormatter[] formatters)
			: this((IEnumerable<IFormatter>)formatters)
		{
		}

		public PacketManager (IEnumerable<IFormatter> formatters)
		{
			this.formatters = formatters.ToDictionary(f => f.PacketType);
		}

		/// <exception cref="ProtocolConnectionException">ConnectProtocolException</exception>
		/// <exception cref="ProtocolViolationException">ProtocolViolationException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task<IPacket> GetPacketAsync (byte[] bytes)
		{
			var packetType = (PacketType)bytes.Byte (0).Bits (4);
			var formatter = default (IFormatter);

			if (!formatters.TryGetValue(packetType, out formatter))
				throw new ProtocolException (Properties.Resources.PacketManager_PacketUnknown);

			var packet = await formatter.FormatAsync (bytes)
				.ConfigureAwait(continueOnCapturedContext: false);

			return packet;
		}

		/// <exception cref="ProtocolConnectionException">ConnectProtocolException</exception>
		/// <exception cref="ProtocolViolationException">ProtocolViolationException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task<byte[]> GetBytesAsync (IPacket packet)
		{
			var formatter = default (IFormatter);

			if (!formatters.TryGetValue(packet.Type, out formatter))
				throw new ProtocolException (Properties.Resources.PacketManager_PacketUnknown);

			var bytes = await formatter.FormatAsync (packet)
				.ConfigureAwait(continueOnCapturedContext: false);

			return bytes;
		}
	}
}
