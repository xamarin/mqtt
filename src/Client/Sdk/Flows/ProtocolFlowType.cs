namespace System.Net.Mqtt.Sdk.Flows
{
	internal enum ProtocolFlowType
	{
		Connect,
		PublishSender,
		PublishReceiver,
		Subscribe,
		Unsubscribe,
		Ping,
		Disconnect
	}
}
