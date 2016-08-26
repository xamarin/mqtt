namespace System.Net.Mqtt.Packets
{
	public enum MqttPacketType : byte
    {
        Connect = 0x01,
        ConnectAck = 0x02,
        Publish = 0x03,
        PublishAck = 0x04,
        PublishReceived = 0x05,
        PublishRelease = 0x06,
        PublishComplete = 0x07,
        Subscribe = 0x08,
        SubscribeAck = 0x09,
        Unsubscribe = 0x0A,
        UnsubscribeAck = 0x0B,
        PingRequest = 0x0C,
        PingResponse = 0x0D,
        Disconnect = 0x0E,
    }
}
