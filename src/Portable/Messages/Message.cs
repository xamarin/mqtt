namespace Hermes.Messages
{
	public abstract class Message : IMessage
    {
        protected Message(MessageType type)
        {
            this.Type = type;
        }

        public MessageType Type { get; private set; }
    }
}
