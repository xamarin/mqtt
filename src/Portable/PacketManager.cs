using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermes.Formatters;
using Hermes.Packets;
using Hermes.Properties;

namespace Hermes
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

		/// <exception cref="ConnectProtocolException">ConnectProtocolException</exception>
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task ManageAsync (byte[] packet)
		{
			var packetType = (PacketType)packet.Byte (0).Bits (4);
			IFormatter formatter;

			if (!formatters.TryGetValue(packetType, out formatter))
				throw new ProtocolException (Resources.PacketManager_PacketUnknown);

			await formatter.ReadAsync (packet);
		}

		/// <exception cref="ConnectProtocolException">ConnectProtocolException</exception>
		/// <exception cref="ViolationProtocolException">ViolationProtocolException</exception>
		/// <exception cref="ProtocolException">ProtocolException</exception>
		public async Task ManageAsync (IPacket packet)
		{
			IFormatter formatter;

			if (!formatters.TryGetValue(packet.Type, out formatter))
				throw new ProtocolException (Resources.PacketManager_PacketUnknown);

			await formatter.WriteAsync (packet);
		}
	}
}
