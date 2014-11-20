namespace Hermes
{
	public partial class ApplicationMessage
	{
		public ApplicationMessage ()
		{

		}

		public ApplicationMessage (string topic, byte[] payload)
		{
			this.Topic = topic;
			this.Payload = payload;
		}

        public string Topic { get; set; }

        public byte[] Payload { get; set; }
	}
}
