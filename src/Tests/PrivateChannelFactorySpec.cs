using Moq;
using System.Net.Mqtt;
using System.Net.Mqtt.Sdk.Bindings;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class PrivateChannelFactorySpec
    {
        [Fact]
        public async Task when_creating_channel_then_succeeds ()
        {
            var factory = new PrivateChannelFactory (Mock.Of<ISubject<PrivateStream>> (), EndpointIdentifier.Client, new MqttConfiguration ());
            var channel = await factory.CreateAsync ();

            Assert.NotNull (channel);
            Assert.True (channel.IsConnected);
        }
    }
}
