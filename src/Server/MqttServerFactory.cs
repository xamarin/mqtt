using System.Diagnostics;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Server.Bindings;
using System.Net.Mqtt.Server.Exceptions;
using System.Net.Mqtt.Storage;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public class MqttServerFactory : IMqttEndpointFactory<IMqttServer>
    {
        static readonly ITracer tracer = Tracer.Get<MqttServerFactory> ();

        readonly IMqttServerBinding binding;
        readonly IMqttAuthenticationProvider authenticationProvider;

        public MqttServerFactory (IMqttAuthenticationProvider authenticationProvider = null)
            : this (new TcpBinding (), authenticationProvider)
        {
        }

        public MqttServerFactory (IMqttServerBinding binding, IMqttAuthenticationProvider authenticationProvider = null)
        {
            this.binding = binding;
            this.authenticationProvider = authenticationProvider ?? NullAuthenticationProvider.Instance;
        }

        /// <exception cref="ServerException">ServerException</exception>
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

                return Task.FromResult<IMqttServer> (new Server (channelProvider, channelFactory,
                    flowProvider, connectionProvider, undeliveredMessagesListener, configuration));
            } catch (Exception ex) {
                tracer.Error (ex, Properties.Resources.Server_InitializeError);

                throw new MqttServerException (Properties.Resources.Server_InitializeError, ex);
            }
        }
    }
}
