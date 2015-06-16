namespace Hermes
{
	public interface IChannelFactory
	{
		IChannel<byte[]> Create ();
	}
}
