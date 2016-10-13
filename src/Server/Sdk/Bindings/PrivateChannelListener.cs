using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace System.Net.Mqtt.Sdk.Bindings
{
    internal class PrivateChannelListener : IMqttChannelListener
    {
        readonly ISubject<PrivateStream> privateStreamListener;
        readonly MqttConfiguration configuration;

        public PrivateChannelListener (ISubject<PrivateStream> privateStreamListener, MqttConfiguration configuration)
        {
            this.privateStreamListener = privateStreamListener;
            this.configuration = configuration;
        }

        public IObservable<IMqttChannel<byte[]>> GetChannelStream ()
        {
            return privateStreamListener
                .Select (stream => new PrivateChannel (stream, EndpointIdentifier.Server, configuration));
        }

        public void Dispose()
        {
            //Nothing to dispose
        }
    }
}
