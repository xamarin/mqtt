namespace System.Net.Mqtt
{
	public interface IChannelFactory
	{
		IChannel<byte[]> Create ();
	}
}
