namespace Hermes.Messages
{
	public class PingResponse : Message
    {
        public PingResponse()
            : base(MessageType.PingResponse)
        {
        }
    }
}
