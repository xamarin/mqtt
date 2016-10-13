using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace System.Net.Mqtt.Sdk.Bindings
{
    internal class PrivateChannelFactory : IMqttChannelFactory
    {
        readonly ISubject<PrivateStream> privateStreamListener;
        readonly EndpointIdentifier identifier;
        readonly MqttConfiguration configuration;

        public PrivateChannelFactory (ISubject<PrivateStream> privateStreamListener, EndpointIdentifier identifier, MqttConfiguration configuration)
        {
            this.privateStreamListener = privateStreamListener;
            this.identifier = identifier;
            this.configuration = configuration;
        }

        public Task<IMqttChannel<byte[]>> CreateAsync ()
        {
            var stream = new PrivateStream (configuration);

            privateStreamListener.OnNext (stream);

            return Task.FromResult<IMqttChannel<byte[]>> (new PrivateChannel (stream, identifier, configuration));
        }
    }
}
