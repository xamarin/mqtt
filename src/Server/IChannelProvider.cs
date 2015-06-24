namespace System.Net.Mqtt.Server
{
	public interface IChannelProvider : IDisposable
	{
		IObservable<IChannel<byte[]>> GetChannels ();
	}
}
