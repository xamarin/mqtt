using System.Reactive.Subjects;

namespace System.Net.Mqtt.Sdk.Bindings
{
	internal interface IServerPrivateBinding : IMqttServerBinding
	{
		ISubject<PrivateStream> PrivateStreamListener { get; }
	}
}
