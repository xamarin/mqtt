using Hermes.Packets;

namespace Hermes
{
	public interface IConnectionProvider
    {
		int Connections { get; }

		void AddConnection (string clientId, IChannel<IPacket> connection);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		IChannel<IPacket> GetConnection (string clientId);

		/// <exception cref="ProtocolException">ProtocolException</exception>
		void RemoveConnection(string clientId);
    }
}
