using System.Threading.Tasks;
using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	public interface IMqttClient : IDisposable
	{
		event EventHandler<MqttEndpointDisconnected> Disconnected;

		string Id { get; }

		bool IsConnected { get; }

		IObservable<MqttApplicationMessage> Receiver { get; }

		/// <exception cref="MqttClientException">ClientException</exception>
		Task ConnectAsync (MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false);

		/// <exception cref="MqttClientException">ClientException</exception>
		Task SubscribeAsync (string topicFilter, MqttQualityOfService qos);

		Task PublishAsync (MqttApplicationMessage message, MqttQualityOfService qos, bool retain = false);

		Task UnsubscribeAsync (params string[] topics);

		Task DisconnectAsync ();
	}
}
