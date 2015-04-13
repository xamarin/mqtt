using System.Collections.Generic;

namespace Hermes
{
	public interface IPacketBuffer
	{
		bool TryGetPackets (byte[] sequence, out IEnumerable<byte[]> packets);
	}
}
