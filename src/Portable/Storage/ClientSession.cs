using System.Collections.Generic;

namespace Hermes.Storage
{
	public class ClientSession
	{
		public ClientSession ()
		{
			this.Subscriptions = new List<ClientSubscription> ();
			//this.SavedMessages = new List<SavedMessage> ();
			this.PendingMessages = new List<PendingMessage> ();
			this.PendingAcknowledgements = new List<PendingAcknowledgement> ();
		}

		public string ClientId { get; set; }

		public bool Clean { get; set; }

		public List<ClientSubscription> Subscriptions { get; set; }

		//public List<SavedMessage> SavedMessages { get; set; }

		public List<PendingMessage> PendingMessages { get; set; }

		public List<PendingAcknowledgement> PendingAcknowledgements { get; set; }
	}
}
 