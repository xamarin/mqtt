namespace System.Net.Mqtt.Server
{
	public interface IMqttChannelListener : IDisposable
	{
		IObservable<IMqttChannel<byte[]>> AcceptChannelsAsync ();
	}
}
