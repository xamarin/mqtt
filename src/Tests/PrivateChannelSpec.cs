using System;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk.Bindings;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class PrivateChannelSpec
    {
        [Fact]
        public void when_creating_channel_with_stream_ready_then_it_is_connected ()
        {
            var configuration = new MqttConfiguration ();
            var stream = new PrivateStream (configuration);
            var channel = new PrivateChannel (stream, EndpointIdentifier.Client, configuration);

            Assert.True (channel.IsConnected);
            Assert.NotNull (channel.ReceiverStream);
            Assert.NotNull (channel.SenderStream);
        }

        [Fact]
        public void when_creating_channel_with_stream_disposed_then_fails ()
        {
            var configuration = new MqttConfiguration ();
            var stream = new PrivateStream (configuration);

            stream.Dispose ();

            var ex = Assert.Throws<ObjectDisposedException> (() => new PrivateChannel (stream, EndpointIdentifier.Client, configuration));

            Assert.NotNull (ex);
         }

        [Fact]
        public async Task when_sending_packet_then_stream_receives_successfully ()
        {
            var configuration = new MqttConfiguration ();
            var stream = new PrivateStream (configuration);
            var channel = new PrivateChannel (stream, EndpointIdentifier.Client, configuration);

            var packetsReceived = 0;

            stream
                .Receive (EndpointIdentifier.Client)
                .Subscribe(packet => {
                    packetsReceived++;
                });

            await channel.SendAsync (new byte[255]);
            await channel.SendAsync (new byte[10]);
            await channel.SendAsync (new byte[34]);
            await channel.SendAsync (new byte[100]);
            await channel.SendAsync (new byte[50]);

            await Task.Delay (TimeSpan.FromMilliseconds(1000));

            Assert.Equal (5, packetsReceived);
        }

        [Fact]
        public async Task when_sending_to_stream_then_channel_receives_successfully ()
        {
            var configuration = new MqttConfiguration ();
            var stream = new PrivateStream (configuration);
            var channel = new PrivateChannel (stream, EndpointIdentifier.Server, configuration);

            var packetsReceived = 0;

            channel.ReceiverStream.Subscribe (packet => {
                packetsReceived++;
            });

            stream.Send (new byte[255], EndpointIdentifier.Client);
            stream.Send (new byte[10], EndpointIdentifier.Client);
            stream.Send (new byte[34], EndpointIdentifier.Client);
            stream.Send (new byte[100], EndpointIdentifier.Client);
            stream.Send (new byte[50], EndpointIdentifier.Client);

            await Task.Delay (TimeSpan.FromMilliseconds (1000));

            Assert.Equal(5, packetsReceived);
        }

        [Fact]
        public async Task when_disposing_channel_then_became_disconnected ()
        {
            var configuration = new MqttConfiguration ();
            var stream = new PrivateStream (configuration);
            var channel = new PrivateChannel (stream, EndpointIdentifier.Server, configuration);

            await channel.CloseAsync ();

            Assert.False (channel.IsConnected);
        }
    }
}
