namespace Hermes.Messages
{
	public class PingResponse : IMessage
    {
		public MessageType Type { get { return MessageType.PingResponse; }}
    }
}
