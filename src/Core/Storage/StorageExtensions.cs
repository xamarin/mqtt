﻿using System.Collections.Generic;
using System.Linq;

namespace System.Net.Mqtt.Storage
{
	internal static class StorageExtensions
	{
		public static IEnumerable<ClientSubscription> GetSubscriptions(this ClientSession session)
		{
			return session.Subscriptions.ToList ();
		}

		public static void AddSubscription(this ClientSession session, ClientSubscription subscription)
		{
			session.Subscriptions.Add(subscription);
		}

		public static void RemoveSubscription(this ClientSession session, ClientSubscription subscription)
		{
			session.Subscriptions.Remove(subscription);
		}

		public static IEnumerable<PendingMessage> GetPendingMessages(this ClientSession session)
		{
			return session.PendingMessages.ToList ();
		}

		public static void AddPendingMessage(this ClientSession session, PendingMessage pending)
		{
			session.PendingMessages.Add(pending);
		}

		public static void RemovePendingMessage(this ClientSession session, PendingMessage pending)
		{
			session.PendingMessages.Remove(pending);
		}

		public static IEnumerable<PendingAcknowledgement> GetPendingAcknowledgements(this ClientSession session)
		{
			return session.PendingAcknowledgements.ToList ();
		}

		public static void AddPendingAcknowledgement(this ClientSession session, PendingAcknowledgement pending)
		{
			session.PendingAcknowledgements.Add(pending);
		}

		public static void RemovePendingAcknowledgement(this ClientSession session, PendingAcknowledgement pending)
		{
			session.PendingAcknowledgements.Remove(pending);
		}
	}
}
