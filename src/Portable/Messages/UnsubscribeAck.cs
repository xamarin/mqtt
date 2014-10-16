namespace Hermes.Messages
{
	public class UnsubscribeAck : Message
    {
        public UnsubscribeAck(ushort messageId)
            : base(MessageType.UnsubscribeAck)
        {
            this.MessageId = messageId;
        }

        public ushort MessageId { get; private set; }
    }
}
