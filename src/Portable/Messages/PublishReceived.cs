namespace Hermes.Messages
{
	public class PublishReceived : Message
    {
        public PublishReceived(ushort messageId)
            : base(MessageType.PublishReceived)
        {
            this.MessageId = messageId;
        }

        public ushort MessageId { get; private set; }
    }
}
