namespace Hermes.Packets
{
	/// <summary>
	/// Protocol flow packets implement the various quality of service levels, 
	/// and are simple packets with a <see cref="PacketType"/> and a 
	/// <see cref="PacketId"/>.
	/// </summary>
	public interface IFlowPacket : IPacket
    {
		/// <summary>
		/// The packet identifier for the specific protocol flow.
		/// </summary>
        ushort PacketId { get; }
    }
}
