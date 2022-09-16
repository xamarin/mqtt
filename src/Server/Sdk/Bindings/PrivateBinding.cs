using System.Reactive.Subjects;

namespace System.Net.Mqtt.Sdk.Bindings
{
	internal class PrivateBinding : IMqttBinding
	{
		readonly EndpointIdentifier identifier;

		public PrivateBinding(ISubject<PrivateStream> privateStreamListener, EndpointIdentifier identifier)
		{
			this.identifier = identifier;
			PrivateStreamListener = privateStreamListener;
		}

		public ISubject<PrivateStream> PrivateStreamListener { get; }

		public IMqttChannelFactory GetChannelFactory(string hostAddress, MqttConfiguration configuration)
			=> new PrivateChannelFactory(PrivateStreamListener, identifier, configuration);
	}
}
