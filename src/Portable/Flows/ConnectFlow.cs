using System;
using Hermes.Packets;

namespace Hermes.Flows
{
	public class ConnectFlow : IProtocolFlow
	{
		public IPacket Apply (IPacket input)
		{
			//TODO: Here it goes the specific logic to produce the output packet (ConnAck in this case) 
			//based on the input packet (Connect in this case)
			throw new NotImplementedException ();
		}
	}
}
