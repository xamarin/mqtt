namespace System.Net.Mqtt
{
	public interface IChannelProvider : IDisposable
	{
		IObservable<IChannel<byte[]>> GetChannels ();
	}
}
