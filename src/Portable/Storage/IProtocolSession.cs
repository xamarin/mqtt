namespace Hermes.Storage
{
	public interface IProtocolSession
	{
		string ClientId { get; }

		bool Clean { get; }
	}
}
