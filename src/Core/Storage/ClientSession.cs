using System.Collections.Generic;

namespace System.Net.Mqtt.Storage
{
	internal class ClientSession : StorageObject
	{
		public ClientSession ()
		{
			this.Subscriptions = new List<ClientSubscription> ();
			this.PendingMessages = new List<PendingMessage> ();
			this.PendingAcknowledgements = new List<PendingAcknowledgement> ();
		}

		public string ClientId { get; set; }

		public bool Clean { get; set; }

		public List<ClientSubscription> Subscriptions { get; set; }

		public List<PendingMessage> PendingMessages { get; set; }

		public List<PendingAcknowledgement> PendingAcknowledgements { get; set; }
	}
}
 