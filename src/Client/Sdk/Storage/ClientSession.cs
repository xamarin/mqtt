using System.Collections.Generic;

namespace System.Net.Mqtt.Sdk.Storage
{
	internal class ClientSession : IStorageObject
	{
		public ClientSession (string clientId, bool clean = false)
		{
			Id = clientId;
			Clean = clean;
			Subscriptions = new List<ClientSubscription> ();
			PendingMessages = new List<PendingMessage> ();
			PendingAcknowledgements = new List<PendingAcknowledgement> ();
		}

		public string Id { get; }

		public bool Clean { get; }

		public List<ClientSubscription> Subscriptions { get; set; }

		public List<PendingMessage> PendingMessages { get; set; }

		public List<PendingAcknowledgement> PendingAcknowledgements { get; set; }
	}
}
