using System;

namespace Hermes.Storage
{
	public class ConnectionWill
	{
		public Guid ConnectionId { get; set; }

		public Packets.Will Will { get; set; }
	}
}
