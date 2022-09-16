using System.Diagnostics;
using System.Net.Mqtt.Sdk.Bindings;
using System.Net.Mqtt.Sdk.Flows;
using System.Net.Mqtt.Sdk.Storage;
using System.Reactive.Subjects;
using ServerProperties = System.Net.Mqtt.Server.Properties;

namespace System.Net.Mqtt.Sdk
{
	/// <summary>
	/// Provides a factory for MQTT Servers
	/// </summary>
	public class MqttServerFactory
	{
		static readonly ITracer tracer = Tracer.Get<MqttServerFactory>();

		readonly IServerPrivateBinding privateBinding;
		readonly IMqttServerBinding binding;
		readonly IMqttAuthenticationProvider authenticationProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="MqttServerFactory" /> class using an in-memory transport protocol binding by default
		/// </summary>
		public MqttServerFactory() : this(binding: null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MqttServerFactory" /> class,
		/// spcifying the transport protocol binding to use and optionally an 
		/// authentication provider to enable authentication as part of the connection mechanism.
		/// </summary>
		/// <param name="binding">
		/// The binding to use as the underlying transport layer.
		/// Possible values: <see cref="ServerTcpBinding"/>, <see cref="ServerWebSocketBinding"/>
		/// See <see cref="IMqttServerBinding"/> for more details about how 
		/// to implement a custom binding
		/// </param>
		/// <param name="authenticationProvider">
		/// Optional authentication provider to use, 
		/// to enable authentication as part of the connection mechanism
		/// See <see cref="IMqttAuthenticationProvider" /> for more details about how to implement
		/// an authentication provider
		/// </param>
		public MqttServerFactory(IMqttServerBinding binding, IMqttAuthenticationProvider authenticationProvider = null)
		{
			privateBinding = new ServerPrivateBinding();
			this.binding = binding;
			this.authenticationProvider = authenticationProvider ?? NullAuthenticationProvider.Instance;
		}

		/// <summary>
		/// Creates an MQTT Server
		/// </summary>
		/// <param name="configuration">
		/// The configuration used for creating the Server
		/// See <see cref="MqttConfiguration" /> for more details about the supported values
		/// </param>
		/// <returns>A new MQTT Server</returns>
		/// <exception cref="MqttServerException">MqttServerException</exception>
		public IMqttServer CreateServer(MqttConfiguration configuration)
		{
			try
			{
				var topicEvaluator = new MqttTopicEvaluator(configuration);
				var packetChannelFactory = new PacketChannelFactory(topicEvaluator, configuration);
				var repositoryProvider = new InMemoryRepositoryProvider();
				var connectionProvider = new ConnectionProvider();
				var packetIdProvider = new PacketIdProvider();
				var undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();
				var flowProvider = new ServerProtocolFlowProvider(authenticationProvider, connectionProvider, topicEvaluator,
					repositoryProvider, packetIdProvider, undeliveredMessagesListener, configuration);

				return new MqttServerImpl(privateBinding, packetChannelFactory, flowProvider, connectionProvider, undeliveredMessagesListener, configuration, binding);
			}
			catch (Exception ex)
			{
				tracer.Error(ex, ServerProperties.Resources.Server_InitializeError);

				throw new MqttServerException(ServerProperties.Resources.Server_InitializeError, ex);
			}
		}
	}
}
