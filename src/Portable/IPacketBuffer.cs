namespace Hermes
{
	public interface IPacketBuffer
	{
		bool TryGetPacket (byte[] sequence, out byte[] packet);
	}
}
