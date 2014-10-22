namespace Hermes.Messages
{
	public class PingRequest : IMessage
    {
		public MessageType Type { get { return MessageType.PingRequest; }}
	}
}
