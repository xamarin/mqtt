namespace Hermes.Messages
{
	public class PublishRelease : Message
    {
        public PublishRelease(ushort messageId)
            : base(MessageType.PublishRelease)
        {
            this.MessageId = messageId;
        }

        public ushort MessageId { get; private set; }

        public bool DuplicatedDelivery { get; private set; }
    }
}
