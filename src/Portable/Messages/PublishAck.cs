namespace Hermes.Messages
{
	public class PublishAck : Message
    {
        public PublishAck(ushort messageId)
            : base(MessageType.PublishAck)
        {
            this.MessageId = messageId;
        }

        public ushort MessageId { get; private set; }
    }
}
