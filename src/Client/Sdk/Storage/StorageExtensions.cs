using System.Collections.Generic;
using System.Linq;

namespace System.Net.Mqtt.Sdk.Storage
{
	internal static class StorageExtensions
	{
		static readonly object subscriptionsLock = new object ();
		static readonly object pendingMessagesLock = new object ();
		static readonly object pendingAcksLock = new object ();

		public static IEnumerable<ClientSubscription> GetSubscriptions (this ClientSession session)
		{
			lock (subscriptionsLock) {
				return session.Subscriptions.ToList ();
			}
		}

		public static void AddSubscription (this ClientSession session, ClientSubscription subscription)
		{
			lock (subscriptionsLock) {
				session.Subscriptions.Add (subscription);
			}
		}

		public static void RemoveSubscription (this ClientSession session, ClientSubscription subscription)
		{
			lock (subscriptionsLock) {
				session.Subscriptions.Remove (subscription);
			}
		}

		public static IEnumerable<PendingMessage> GetPendingMessages (this ClientSession session)
		{
			lock (pendingMessagesLock) {
				return session.PendingMessages.ToList ();
			}
		}

		public static void AddPendingMessage (this ClientSession session, PendingMessage pending)
		{
			lock (pendingMessagesLock) {
				session.PendingMessages.Add (pending);
			}
		}

		public static void RemovePendingMessage (this ClientSession session, PendingMessage pending)
		{
			lock (pendingMessagesLock) {
				session.PendingMessages.Remove (pending);
			}
		}

		public static IEnumerable<PendingAcknowledgement> GetPendingAcknowledgements (this ClientSession session)
		{
			lock (pendingAcksLock) {
				return session.PendingAcknowledgements.ToList ();
			}
		}

		public static void AddPendingAcknowledgement (this ClientSession session, PendingAcknowledgement pending)
		{
			lock (pendingAcksLock) {
				session.PendingAcknowledgements.Add (pending);
			}
		}

		public static void RemovePendingAcknowledgement (this ClientSession session, PendingAcknowledgement pending)
		{
			lock (pendingAcksLock) {
				session.PendingAcknowledgements.Remove (pending);
			}
		}
	}
}
