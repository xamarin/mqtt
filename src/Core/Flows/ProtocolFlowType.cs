namespace System.Net.Mqtt.Flows
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
