using System.Collections.Generic;

namespace Hermes.Messages
{
	public class SubscribeAck : Message
    {
        public SubscribeAck(ushort messageId, params SubscribeReturnCode[] returnCodes)
            : this(messageId, (IEnumerable<SubscribeReturnCode>)returnCodes)
        {
        }

        public SubscribeAck(ushort messageId, IEnumerable<SubscribeReturnCode> returnCodes)
            : base(MessageType.SubscribeAck)
        {
            this.MessageId = messageId;
            this.ReturnCodes = returnCodes;
        }

        public ushort MessageId { get; private set; }

        public IEnumerable<SubscribeReturnCode> ReturnCodes { get; private set; }
    }
}
