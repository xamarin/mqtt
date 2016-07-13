using System.Reactive;
using System.Net.Mqtt.Flows;
using System.Net.Mqtt.Storage;
using System.Net.Mqtt.Diagnostics;
using Merq;

namespace System.Net.Mqtt.Server
{
    public class ServerFactory : IFactory<Server>
    {
        readonly ITracerManager tracerManager;
        readonly ITracer tracer;
        readonly IProtocolBinding binding;
        readonly IAuthenticationProvider authenticationProvider;

        public ServerFactory (IProtocolBinding binding, IAuthenticationProvider authenticationProvider = null)
             : this (binding, new DefaultTracerManager (), authenticationProvider)
        {
            this.authenticationProvider = authenticationProvider ?? NullAuthenticationProvider.Instance;
        }

        public ServerFactory (IProtocolBinding binding, ITracerManager tracerManager, IAuthenticationProvider authenticationProvider = null)
        {
            tracer = tracerManager.Get<ServerFactory> ();
            this.tracerManager = tracerManager;
            this.binding = binding;
            this.authenticationProvider = authenticationProvider ?? NullAuthenticationProvider.Instance;
        }

        /// <exception cref="ServerException">ServerException</exception>
        public Server Create (ProtocolConfiguration configuration)
        {
            try {
                var topicEvaluator = new TopicEvaluator (configuration);
                var channelProvider = binding.GetChannelProvider (tracerManager, configuration);
                var channelFactory = new PacketChannelFactory (topicEvaluator, tracerManager, configuration);
                var repositoryProvider = new InMemoryRepositoryProvider ();
                var connectionProvider = new ConnectionProvider (tracerManager);
                var packetIdProvider = new PacketIdProvider ();
                var eventStream = new EventStream ();
                var flowProvider = new ServerProtocolFlowProvider (authenticationProvider, connectionProvider, topicEvaluator,
                repositoryProvider, packetIdProvider, eventStream, tracerManager, configuration);

                return new Server (channelProvider, channelFactory,
                    flowProvider, connectionProvider, eventStream, tracerManager, configuration);
            } catch (Exception ex) {
                tracer.Error (ex, Properties.Resources.Server_InitializeError);

                throw new ServerException (Properties.Resources.Server_InitializeError, ex);
            }
        }
    }
}
