using System.Collections.Generic;

namespace Hermes.Storage
{
	public class ClientSession
	{
		public ClientSession ()
		{
			this.Subscriptions = new List<ClientSubscription> ();
			this.PendingMessages = new List<PendingMessage> ();
		}

		public string ClientId { get; set; }

		public bool Clean { get; set; }

		public List<ClientSubscription> Subscriptions { get; set; }

		public List<PendingMessage> PendingMessages { get; set; }
	}
}
 