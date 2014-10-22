namespace Hermes.Messages
{
	/// <summary>
	/// Protocol flow messages implement the various quality of service levels, 
	/// and are simple messages with a <see cref="MessageType"/> and a 
	/// <see cref="MessageId"/>.
	/// </summary>
	public interface IFlowMessage : IMessage
    {
		/// <summary>
		/// The message identifier for the specific protocol flow.
		/// </summary>
        ushort MessageId { get; }
    }
}
