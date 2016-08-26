using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Server.Bindings;
using System.Net.Mqtt.Server.Exceptions;
using System.Net.Mqtt.Storage;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Server
{
    public class MqttServerFactory : IMqttEndpointFactory<IMqttServer>
    {
        readonly ITracerManager tracerManager;
        readonly ITracer tracer;
        readonly IMqttServerBinding binding;
        readonly IMqttAuthenticationProvider authenticationProvider;

        public MqttServerFactory (IMqttServerBinding binding, IMqttAuthenticationProvider authenticationProvider = null)
             : this (binding, new DefaultTracerManager (), authenticationProvider)
        {
            this.authenticationProvider = authenticationProvider ?? NullAuthenticationProvider.Instance;
        }

        public MqttServerFactory (IMqttServerBinding binding, ITracerManager tracerManager, IMqttAuthenticationProvider authenticationProvider = null)
        {
            tracer = tracerManager.Get<MqttServerFactory> ();
            this.tracerManager = tracerManager;
            this.binding = binding;
            this.authenticationProvider = authenticationProvider ?? NullAuthenticationProvider.Instance;
        }

        /// <exception cref="ServerException">ServerException</exception>
        public Task<IMqttServer> CreateAsync (MqttConfiguration configuration)
        {
            try {
                var topicEvaluator = new MqttTopicEvaluator (configuration);
                var channelProvider = binding.GetChannelProvider (tracerManager, configuration);
                var channelFactory = new PacketChannelFactory (topicEvaluator, tracerManager, configuration);
                var repositoryProvider = new InMemoryRepositoryProvider ();
                var connectionProvider = new ConnectionProvider (tracerManager);
                var packetIdProvider = new PacketIdProvider ();
                var eventStream = new EventStream ();
                var flowProvider = new ServerProtocolFlowProvider (authenticationProvider, connectionProvider, topicEvaluator,
                    repositoryProvider, packetIdProvider, eventStream, tracerManager, configuration);

                return Task.FromResult<IMqttServer> (new Server (channelProvider, channelFactory,
                    flowProvider, connectionProvider, eventStream, tracerManager, configuration));
            } catch (Exception ex) {
                tracer.Error (ex, Resources.Server_InitializeError);

                throw new MqttServerException (Resources.Server_InitializeError, ex);
            }
        }
    }
}
