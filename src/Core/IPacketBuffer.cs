using System.Collections.Generic;

namespace System.Net.Mqtt
{
	public interface IPacketBuffer
	{
		bool TryGetPackets (byte[] sequence, out IEnumerable<byte[]> packets);
	}
}
