using System;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk.Bindings;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class PrivateStreamSpec
    {
        [Fact]
        public void when_creating_stream_then_becomes_ready ()
        {
            var configuration = new MqttConfiguration ();
            var stream = new PrivateStream (configuration);

            Assert.True (!stream.IsDisposed);
        }

        [Fact]
        public async Task when_sending_payload_with_identifier_then_receives_on_same_identifier ()
        {
            var configuration = new MqttConfiguration ();
            var stream = new PrivateStream (configuration);

            var clientReceived = 0;
            var clientReceiver = stream
                .Receive (EndpointIdentifier.Client)
                .Subscribe (payload => {
                    clientReceived++;
                });

            var serverReceived = 0;
            var serverReceiver = stream
                .Receive (EndpointIdentifier.Server)
                .Subscribe (payload => {
                    serverReceived++;
                });

            stream.Send (new byte[255], EndpointIdentifier.Client);
            stream.Send (new byte[100], EndpointIdentifier.Client);
            stream.Send (new byte[30], EndpointIdentifier.Client);
            stream.Send (new byte[10], EndpointIdentifier.Server);
            stream.Send (new byte[500], EndpointIdentifier.Server);
            stream.Send (new byte[5], EndpointIdentifier.Server);

            await Task.Delay (TimeSpan.FromMilliseconds (1000));

            Assert.Equal (3, clientReceived);
            Assert.Equal (3, serverReceived);
        }

        [Fact]
        public async Task when_sending_payload_with_identifier_then_does_not_receive_on_other_identifier()
        {
            var configuration = new MqttConfiguration ();
            var stream = new PrivateStream (configuration);

            var serverReceived = 0;
            var serverReceiver = stream
                .Receive (EndpointIdentifier.Server)
                .Subscribe (payload => {
                    serverReceived++;
                });

            stream.Send (new byte[255], EndpointIdentifier.Client);
            stream.Send (new byte[100], EndpointIdentifier.Client);
            stream.Send (new byte[30], EndpointIdentifier.Client);

            await Task.Delay (TimeSpan.FromMilliseconds(1000));

            Assert.Equal (0, serverReceived);
        }

        [Fact]
        public void when_disposing_stream_then_becomes_not_ready ()
        {
            var configuration = new MqttConfiguration();
            var stream = new PrivateStream (configuration);

            stream.Dispose ();

            Assert.True(stream.IsDisposed);
        }
    }
}
