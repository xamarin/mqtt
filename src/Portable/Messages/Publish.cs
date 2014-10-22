using System;
using System.Linq;

namespace Hermes.Messages
{
	public class Publish : Message, IEquatable<Publish>
    {
        public Publish(QualityOfService qualityOfService, bool retain, string topic, ushort? messageId = null, bool duplicatedDelivery = false)
            : base(MessageType.Publish)
        {
			if (qualityOfService == QualityOfService.AtMostOnce && messageId != null)
                throw new ArgumentException();

			if (qualityOfService == QualityOfService.AtMostOnce && duplicatedDelivery) {
				throw new ArgumentException ();
			}

            if (qualityOfService != QualityOfService.AtMostOnce && messageId == null)
                throw new ArgumentNullException();
			
            this.QualityOfService = qualityOfService;
			this.DuplicatedDelivery = duplicatedDelivery;
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

		public bool Equals (Publish other)
		{
			if (other == null)
				return false;

			var equals = this.QualityOfService == other.QualityOfService &&
				this.DuplicatedDelivery == other.DuplicatedDelivery &&
				this.Retain == other.Retain &&
				this.Topic == other.Topic &&
				this.MessageId == other.MessageId;

			if(this.Payload != null) {
				equals &= this.Payload.ToList().SequenceEqual(other.Payload);
			}

			return equals;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var publish = obj as Publish;

			if (publish == null)
				return false;

			return this.Equals (publish);
		}

		public static bool operator == (Publish publish, Publish other)
		{
			if ((object)publish == null || (object)other == null)
				return Object.Equals(publish, other);

			return publish.Equals(other);
		}

		public static bool operator != (Publish publish, Publish other)
		{
			if ((object)publish == null || (object)other == null)
				return !Object.Equals(publish, other);

			return !publish.Equals(other);
		}

		public override int GetHashCode ()
		{
			var hashCode = this.QualityOfService.GetHashCode() +
				this.DuplicatedDelivery.GetHashCode() +
				this.Retain.GetHashCode() +
				this.Topic.GetHashCode () +
				BitConverter.ToString (this.Payload).GetHashCode ();

			if (this.MessageId.HasValue) {
				hashCode += this.MessageId.Value.GetHashCode ();
			}

			return hashCode;
		}
	}
}
