using System.Diagnostics;
using System.Net.Mqtt.Bindings;
using System.Net.Mqtt.Exceptions;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Storage;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ServerProperties = System.Net.Mqtt.Server.Properties;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Provides a factory for MQTT Brokers
    /// </summary>
    public class MqttServerFactory : IMqttEndpointFactory<IMqttServer>
    {
        static readonly ITracer tracer = Tracer.Get<MqttServerFactory> ();

        readonly IMqttServerBinding binding;
        readonly IMqttAuthenticationProvider authenticationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttServerFactory" /> class,
        /// with the option to define an authentication provider to enable authentication
        /// as part of the connection mechanism.
        /// It also uses TCP as the default transport protocol binding
        /// </summary>
        /// <param name="authenticationProvider">
        /// Optional authentication provider to use, 
        /// to enable authentication as part of the connection mechanism
        /// See <see cref="IMqttAuthenticationProvider" /> for more details about how to implement
        /// an authentication provider
        /// </param>
        public MqttServerFactory (IMqttAuthenticationProvider authenticationProvider = null)
            : this (new ServerTcpBinding (), authenticationProvider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttServerFactory" /> class,
        /// spcifying the transport protocol binding to use and optionally the 
        /// authentication provider to enable authentication as part of the connection mechanism.
        /// </summary>
        /// <param name="binding">
        /// Transport protocol binding to use as the MQTT underlying protocol
        /// See <see cref="IMqttServerBinding" /> for more details about how to implement it 
        /// </param>
        /// <param name="authenticationProvider">
        /// Optional authentication provider to use, 
        /// to enable authentication as part of the connection mechanism
        /// See <see cref="IMqttAuthenticationProvider" /> for more details about how to implement
        /// an authentication provider
        /// </param>
        public MqttServerFactory (IMqttServerBinding binding, IMqttAuthenticationProvider authenticationProvider = null)
        {
            this.binding = binding;
            this.authenticationProvider = authenticationProvider ?? NullAuthenticationProvider.Instance;
        }

        /// <summary>
        /// Creates an MQTT Broker
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for creating the Broker
        /// See <see cref="MqttConfiguration" /> for more details about the supported values
        /// </param>
        /// <returns>A new MQTT Broker</returns>
        /// <exception cref="MqttServerException">MqttServerException</exception>
        public Task<IMqttServer> CreateAsync (MqttConfiguration configuration)
        {
            try {
                var topicEvaluator = new MqttTopicEvaluator (configuration);
                var channelProvider = binding.GetChannelListener (configuration);
                var channelFactory = new PacketChannelFactory (topicEvaluator, configuration);
                var repositoryProvider = new InMemoryRepositoryProvider ();
                var connectionProvider = new ConnectionProvider ();
                var packetIdProvider = new PacketIdProvider ();
                var undeliveredMessagesListener = new Subject<MqttUndeliveredMessage> ();
                var flowProvider = new ServerProtocolFlowProvider (authenticationProvider, connectionProvider, topicEvaluator,
                    repositoryProvider, packetIdProvider, undeliveredMessagesListener, configuration);

                return Task.FromResult<IMqttServer> (new MqttServer (channelProvider, channelFactory,
                    flowProvider, connectionProvider, undeliveredMessagesListener, configuration));
            } catch (Exception ex) {
                tracer.Error (ex, ServerProperties.Resources.Server_InitializeError);

                throw new MqttServerException (ServerProperties.Resources.Server_InitializeError, ex);
            }
        }
    }
}
