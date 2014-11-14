namespace Hermes
{
	public interface ISocketFactory
	{
		IBufferedChannel<byte> CreateSocket (string serverAddress);
	}
}
