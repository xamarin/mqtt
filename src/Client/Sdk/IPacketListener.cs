using System.Net.Mqtt.Sdk.Packets;

namespace System.Net.Mqtt.Sdk
{
	internal interface IPacketListener : IDisposable
	{
		IObservable<IPacket> PacketStream { get; }

		void Listen ();
	}
}
