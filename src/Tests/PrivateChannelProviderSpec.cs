using System;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk.Bindings;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class PrivateChannelProviderSpec
    {
        [Fact]
        public async Task when_gettting_channels_with_stream_then_succeeds ()
        {
            var configuration = new MqttConfiguration ();
            var privateStreamListener = new Subject<PrivateStream> ();
            var provider = new PrivateChannelListener (privateStreamListener, configuration);

            var channelsCreated = 0;

            provider
                .GetChannelStream ()
                .Subscribe (channel => {
                    channelsCreated++;
                });

            privateStreamListener.OnNext (new PrivateStream (configuration));
            privateStreamListener.OnNext (new PrivateStream (configuration));
            privateStreamListener.OnNext (new PrivateStream (configuration));

            await Task.Delay (TimeSpan.FromMilliseconds(1000));

            Assert.Equal (3, channelsCreated);
        }

        [Fact]
        public async Task when_gettting_channels_without_stream_then_fails ()
        {
            var configuration = new MqttConfiguration ();
            var privateStreamListener = new Subject<PrivateStream> ();
            var provider = new PrivateChannelListener (privateStreamListener, configuration);

            var channelsCreated = 0;

            provider
                .GetChannelStream ()
                .Subscribe (channel => {
                    channelsCreated++;
                });

            await Task.Delay (TimeSpan.FromMilliseconds(1000));

            Assert.Equal (0, channelsCreated);
        }
    }
}
