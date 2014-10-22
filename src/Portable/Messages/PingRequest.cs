namespace Hermes.Messages
{
	public class PingRequest : Message
    {
        public PingRequest()
            : base(MessageType.PingRequest)
        {
        }
	}
}
