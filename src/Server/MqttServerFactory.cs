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

        public MqttServerFactory (IMqttAuthenticationProvider authenticationProvider = null)
            : this (new ServerTcpBinding (), authenticationProvider)
        {
        }

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
