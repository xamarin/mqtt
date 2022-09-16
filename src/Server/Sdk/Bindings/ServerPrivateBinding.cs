using System.Reactive.Subjects;

namespace System.Net.Mqtt.Sdk.Bindings
{
	internal class ServerPrivateBinding : PrivateBinding, IServerPrivateBinding
	{
		public ServerPrivateBinding() : base(privateStreamListener: new Subject<PrivateStream>(), identifier: EndpointIdentifier.Server)
		{
		}

		public IMqttChannelListener GetChannelListener(MqttConfiguration configuration)
			=> new PrivateChannelListener(PrivateStreamListener, configuration);
	}
}
