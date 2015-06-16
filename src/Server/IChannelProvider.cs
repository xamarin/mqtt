using System;

namespace Hermes
{
	public interface IChannelProvider : IDisposable
	{
		IObservable<IChannel<byte[]>> GetChannels ();
	}
}
