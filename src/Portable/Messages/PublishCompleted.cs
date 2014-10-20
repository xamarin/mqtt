namespace Hermes.Messages
{
	public class PublishCompleted : Message
    {
        public PublishCompleted(ushort messageId)
            : base(MessageType.PublishCompleted)
        {
            this.MessageId = messageId;
        }

        public ushort MessageId { get; private set; }
    }
}
