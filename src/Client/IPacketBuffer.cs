using System.Collections.Generic;

namespace System.Net.Mqtt
{
	internal interface IPacketBuffer
	{
		bool TryGetPackets (byte[] sequence, out IEnumerable<byte[]> packets);
	}
}
