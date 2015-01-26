using System;
using Hermes.Packets;

namespace Hermes
{
	public interface IPacketListener
	{
		IObservable<IPacket> Packets { get; }

		void Listen (IChannel<IPacket> channel);
	}
}
