using System;

namespace Hermes.Messages
{
	public class Publish : Message
    {
        public Publish(QualityOfService qos, bool retain, string topic, ushort? messageId = null, bool duplicated = false)
            : base(MessageType.Publish)
        {
			if (qos == QualityOfService.AtMostOnce && messageId != null)
                throw new ArgumentException();

			if (qos == QualityOfService.AtMostOnce && duplicated) {
				throw new ArgumentException ();
			}

            if (qos != QualityOfService.AtMostOnce && messageId == null)
                throw new ArgumentNullException();
			
            this.QualityOfService = qos;
			this.DuplicatedDelivery = duplicated;
			this.Retain = retain;
			this.Topic = topic;
            this.MessageId = messageId;
        }

		public QualityOfService QualityOfService { get; private set; }

		public bool DuplicatedDelivery { get; private set; }

		public bool Retain { get; private set; }

        public string Topic { get; private set; }

        public ushort? MessageId { get; private set; }

		public byte[] Payload { get; set; }
    }
}
