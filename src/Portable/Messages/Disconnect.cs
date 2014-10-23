namespace Hermes.Messages
{
	public class Disconnect : IMessage
    {
		public MessageType Type { get { return MessageType.Disconnect; }}
    }
}
