using System;
using System.Threading.Tasks;
using Hermes.Packets;

namespace Hermes
{
	public interface IClient
	{
		event EventHandler<ClosedEventArgs> Closed;

		string Id { get; }

		bool IsConnected { get; }

		IObservable<ApplicationMessage> Receiver { get; }

		IObservable<IPacket> Sender { get; }

		/// <exception cref="ClientException">ClientException</exception>
		Task ConnectAsync (ClientCredentials credentials, bool cleanSession = false);

		/// <exception cref="ClientException">ClientException</exception>
		Task ConnectAsync (ClientCredentials credentials, Will will, bool cleanSession = false);

		/// <exception cref="ClientException">ClientException</exception>
		Task SubscribeAsync (string topicFilter, QualityOfService qos);

		Task PublishAsync (ApplicationMessage message, QualityOfService qos, bool retain = false);

		Task UnsubscribeAsync (params string[] topics);

		Task DisconnectAsync ();

		void Close ();
	}
}
