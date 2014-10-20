using System.Collections.Generic;

namespace Hermes.Messages
{
	public class Subscribe : Message
    {
        public Subscribe(ushort messageId, string topic, QualityOfService requestedQos)
            : this(messageId, new [] { new Subscription(topic, requestedQos) })
        {
        }

        public Subscribe(ushort messageId, params Subscription[] subscriptions)
            : this(messageId, (IEnumerable<Subscription>)subscriptions)
        {
        }

        public Subscribe(ushort messageId, IEnumerable<Subscription> subscriptions)
            : base(MessageType.Subscribe)
        {
            this.MessageId = messageId;
            this.Subscriptions = subscriptions;
        }

        public ushort MessageId { get; private set; }

        public IEnumerable<Subscription> Subscriptions { get; private set; }

        public bool DuplicatedDelivery { get; private set; }
    }
}
