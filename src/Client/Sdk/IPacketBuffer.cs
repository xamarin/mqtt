using System.Collections.Generic;

namespace System.Net.Mqtt.Sdk
{
    internal interface IPacketBuffer
	{
		bool TryGetPackets (IEnumerable<byte> sequence, out IEnumerable<byte[]> packets);
	}
}
