using System;
using System.Collections.Generic;
using System.Linq;

namespace Hermes.Messages
{
	public class Unsubscribe : Message
    {
        public Unsubscribe(ushort messageId, params string[] topics)
            : this(messageId, (IEnumerable<string>)topics)
        {

        }

        public Unsubscribe(ushort messageId, IEnumerable<string> topics)
            : base(MessageType.Unsubscribe)
        {
			if (!topics.Any ())
				throw new ArgumentException ();

            this.MessageId = messageId;
            this.Topics = topics;
        }

        public ushort MessageId { get; private set; }

        public IEnumerable<string> Topics { get; private set; }

        public bool DuplicatedDelivery { get; private set; }
    }
}
