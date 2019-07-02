using System.Threading.Tasks;

namespace System.Net.Mqtt
{
	/// <summary>
	/// Represents an MQTT Client
	/// </summary>
	public interface IMqttClient : IDisposable
	{
		/// <summary>
		/// Event raised when the Client gets disconnected.
		/// The Client disconnection could be caused by a protocol disconnect, 
		/// an error or a remote disconnection produced by the Server.
		/// See <see cref="MqttEndpointDisconnected"/> for more details on the disconnection information
		/// </summary>
		event EventHandler<MqttEndpointDisconnected> Disconnected;

		/// <summary>
		/// Id of the connected Client.
		/// This Id correspond to the <see cref="MqttClientCredentials.ClientId"/> parameter passed to 
		/// <see cref="ConnectAsync (MqttClientCredentials, MqttLastWill, bool)"/> method
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Indicates if the Client is connected by protocol.
		/// This means that a CONNECT packet has been sent, 
		/// by calling <see cref="ConnectAsync (MqttClientCredentials, MqttLastWill, bool)"/> method
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// Represents the incoming application messages received from the Server
		/// These messages correspond to the topics subscribed,
		/// by calling <see cref="SubscribeAsync(string, MqttQualityOfService)"/> method 
		/// See <see cref="MqttApplicationMessage"/> for more details about the application messages
		/// </summary>
		/// <remarks>
		/// The stream lifecycle ends when the client is disconnected, either by a protocol disconnect, 
		/// an error or a remote disconnection produced by the Server.
		/// The stream subscritpions should be created again if the client is intended to be re-connected
		/// </remarks>
		IObservable<MqttApplicationMessage> MessageStream { get; }

		/// <summary>
		/// Represents the protocol connection, which consists of sending a CONNECT packet
		/// and awaiting the corresponding CONNACK packet from the Server
		/// </summary>
		/// <param name="credentials">
		/// The credentials used to connect to the Server. 
		/// See <see cref="MqttClientCredentials" /> for more details on the credentials information
		/// </param>
		/// <param name="will">
		/// The last will message to send from the Server when an unexpected Client disconnection occurrs. 
		/// See <see cref="MqttLastWill" /> for more details about the will message structure
		/// </param>
		/// <param name="cleanSession">
		/// Indicates if the session state between Client and Server must be cleared between connections
		/// Defaults to false, meaning that session state will be preserved by default accross connections
		/// </param>
		/// <returns>
		/// Returns the state of the client session created as part of the connection
		/// See <see cref="SessionState" /> for more details about the session state values
		/// </returns>
		/// <exception cref="MqttClientException">MqttClientException</exception>
		/// <remarks>
		/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180841">MQTT Connect</a>
		/// for more details about the protocol connection
		/// </remarks>
		Task<SessionState> ConnectAsync(MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false);

		/// <summary>
		/// Represents the protocol connection, which consists of sending a CONNECT packet
		/// and awaiting the corresponding CONNACK packet from the Server.
		/// This overload allows to connect without specifying a client id, in which case a random id will be generated
		/// according to the specification of the protocol.
		/// </summary>
		/// <param name="will">
		/// The last will message to send from the Server when an unexpected Client disconnection occurrs. 
		/// See <see cref="MqttLastWill" /> for more details about the will message structure
		/// </param>
		/// /// <returns>
		/// Returns the state of the client session created as part of the connection
		/// See <see cref="SessionState" /> for more details about the session state values
		/// </returns>
		/// <exception cref="MqttClientException">MqttClientException</exception>
		/// <remarks>
		/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180841">MQTT Connect</a>
		/// for more details about the protocol connection
		/// </remarks>
		Task<SessionState> ConnectAsync(MqttLastWill will = null);

		/// <summary>
		/// Represents the protocol subscription, which consists of sending a SUBSCRIBE packet
		/// and awaiting the corresponding SUBACK packet from the Server
		/// </summary>
		/// <param name="topicFilter">
		/// The topic to subscribe for incoming application messages. 
		/// Every message sent by the Server that matches a subscribed topic, will go to the <see cref="MessageStream"/> 
		/// </param>
		/// <param name="qos">
		/// The maximum Quality Of Service (QoS) that the Server should maintain when publishing application messages for the subscribed topic to the Client
		/// This QoS is maximum because it depends on the QoS supported by the Server. 
		/// See <see cref="MqttQualityOfService" /> for more details about the QoS values
		/// </param>
		/// <exception cref="MqttClientException">MqttClientException</exception>
		/// <remarks>
		/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180876">MQTT Subscribe</a>
		/// for more details about the protocol subscription
		/// </remarks>
		Task SubscribeAsync(string topicFilter, MqttQualityOfService qos);

		/// <summary>
		/// Represents the protocol publish, which consists of sending a PUBLISH packet
		/// and awaiting the corresponding ACK packet, if applies, based on the QoS defined
		/// </summary>
		/// <param name="message">
		/// The application message to publish to the Server.
		/// See <see cref="MqttApplicationMessage" /> for more details about the application messages
		/// </param>
		/// <param name="qos">
		/// The Quality Of Service (QoS) associated to the application message, which determines 
		/// the sequence of acknowledgements that Client and Server should send each other to consider the message as delivered
		/// See <see cref="MqttQualityOfService" /> for more details about the QoS values
		/// </param>
		/// <param name="retain">
		/// Indicates if the application message should be retained by the Server for future subscribers.
		/// Only the last message of each topic is retained
		/// </param>
		/// <remarks>
		/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180850">MQTT Publish</a>
		/// for more details about the protocol publish
		/// </remarks>
		Task PublishAsync(MqttApplicationMessage message, MqttQualityOfService qos, bool retain = false);

		/// <summary>
		/// Represents the protocol unsubscription, which consists of sending an UNSUBSCRIBE packet
		/// and awaiting the corresponding UNSUBACK packet from the Server
		/// </summary>
		/// <param name="topics">
		/// The list of topics to unsubscribe from
		/// Once the unsubscription completes, no more application messages for those topics will arrive to <see cref="MessageStream"/> 
		/// </param>
		/// <exception cref="MqttClientException">MqttClientException</exception>
		/// <remarks>
		/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180885">MQTT Unsubscribe</a>
		/// for more details about the protocol unsubscription
		/// </remarks>
		Task UnsubscribeAsync(params string[] topics);

		/// <summary>
		/// Represents the protocol disconnection, which consists of sending a DISCONNECT packet to the Server
		/// No acknowledgement is sent by the Server on the disconnection
		/// Once the client is successfully disconnected, the <see cref="Disconnected"/> event will be fired 
		/// </summary>
		/// <remarks>
		/// See <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180903">MQTT Disconnect</a>
		/// for more details about the protocol disconnection
		/// </remarks>
		Task DisconnectAsync();
	}
}
