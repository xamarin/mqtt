using System;
using Hermes.Messages;

namespace Hermes.Flows
{
	public class ConnectFlow : IProtocolFlow
	{
		public IMessage Apply (IMessage input)
		{
			//TODO: Here it goes the specific logic to produce the output message (ConnAck in this case) 
			//based on the input message (Connect in this case)
			throw new NotImplementedException ();
		}
	}
}
