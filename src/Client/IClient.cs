using System.Threading.Tasks;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	public interface IClient
	{
		event EventHandler<ClosedEventArgs> Closed;

		string Id { get; }

		bool IsConnected { get; }

		IObservable<ApplicationMessage> Receiver { get; }

		IObservable<IPacket> Sender { get; }

		/// <exception cref="ClientException">ClientException</exception>
		Task ConnectAsync (ClientCredentials credentials, Will will = null, bool cleanSession = false);

		/// <exception cref="ClientException">ClientException</exception>
		Task SubscribeAsync (string topicFilter, QualityOfService qos);

		Task PublishAsync (ApplicationMessage message, QualityOfService qos, bool retain = false);

		Task UnsubscribeAsync (params string[] topics);

		Task DisconnectAsync ();

		void Close ();
	}
}
