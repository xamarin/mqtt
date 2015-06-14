using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	public interface IPacketListener : IDisposable
	{
		IObservable<IPacket> Packets { get; }

		void Listen ();
	}
}
