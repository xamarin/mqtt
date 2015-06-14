using System;
using Hermes.Packets;

namespace Hermes
{
	public interface IPacketListener : IDisposable
	{
		IObservable<IPacket> Packets { get; }

		void Listen ();
	}
}
