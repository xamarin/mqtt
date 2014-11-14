namespace Hermes
{
	public class ApplicationMessage
	{
		public ApplicationMessage(string topic, byte[] message)
        {
            this.Topic = topic;
            this.Message = message;
        }

        public string Topic { get; set; }

        public byte[] Message { get; set; }
	}
}
