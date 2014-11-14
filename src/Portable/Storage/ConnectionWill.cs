using System;

namespace Hermes.Storage
{
	public class ConnectionWill
	{
		public string ClientId { get; set; }

		public Packets.Will Will { get; set; }
	}
}
