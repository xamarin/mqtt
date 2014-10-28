namespace Hermes.Storage
{
	public class ProtocolSession : IProtocolSession
	{
		public string ClientId { get; set; }

		public bool Clean { get; set; }
	}
}
