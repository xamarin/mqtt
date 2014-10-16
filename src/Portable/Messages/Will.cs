namespace Hermes.Messages
{
	public class Will
    {
        public Will(string topic, QualityOfService qos, bool retain, string message)
        {
            this.Topic = topic;
            this.QualityOfService = qos;
            this.Retain = retain;
			this.Message = message;
        }

        public string Topic { get; private set; }

        public QualityOfService QualityOfService { get; private set; }

		public bool Retain { get; private set; }

		public string Message { get; private set; }
    }
}
