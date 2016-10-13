using System.Collections.Generic;

namespace System.Net.Mqtt.Sdk.Storage
{
	internal class ClientSession : StorageObject
	{
		public ClientSession ()
		{
			Subscriptions = new List<ClientSubscription> ();
			PendingMessages = new List<PendingMessage> ();
			PendingAcknowledgements = new List<PendingAcknowledgement> ();
		}

		public string ClientId { get; set; }

		public bool Clean { get; set; }

		public List<ClientSubscription> Subscriptions { get; set; }

		public List<PendingMessage> PendingMessages { get; set; }

		public List<PendingAcknowledgement> PendingAcknowledgements { get; set; }
	}
}
