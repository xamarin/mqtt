namespace System.Net.Mqtt.Sdk.Packets
{
    /// <summary>
    /// Represents one of the possible MQTT packet types
    /// </summary>
	internal enum MqttPacketType : byte
    {
        /// <summary>
        /// MQTT CONNECT packet
        /// </summary>
        Connect = 0x01,
        /// <summary>
        /// MQTT CONNACK packet
        /// </summary>
        ConnectAck = 0x02,
        /// <summary>
        /// MQTT PUBLISH packet
        /// </summary>
        Publish = 0x03,
        /// <summary>
        /// MQTT PUBACK packet
        /// </summary>
        PublishAck = 0x04,
        /// <summary>
        /// MQTT PUBREC packet
        /// </summary>
        PublishReceived = 0x05,
        /// <summary>
        /// MQTT PUBREL packet
        /// </summary>
        PublishRelease = 0x06,
        /// <summary>
        /// MQTT PUBCOMP packet
        /// </summary>
        PublishComplete = 0x07,
        /// <summary>
        /// MQTT SUBSCRIBE packet
        /// </summary>
        Subscribe = 0x08,
        /// <summary>
        /// MQTT SUBACK packet
        /// </summary>
        SubscribeAck = 0x09,
        /// <summary>
        /// MQTT UNSUBSCRIBE packet
        /// </summary>
        Unsubscribe = 0x0A,
        /// <summary>
        /// MQTT UNSUBACK packet
        /// </summary>
        UnsubscribeAck = 0x0B,
        /// <summary>
        /// MQTT PINGREQ packet
        /// </summary>
        PingRequest = 0x0C,
        /// <summary>
        /// MQTT PINGRESP packet
        /// </summary>
        PingResponse = 0x0D,
        /// <summary>
        /// MQTT DISCONNECT packet
        /// </summary>
        Disconnect = 0x0E,
    }
}
