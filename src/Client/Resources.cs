namespace System.Net.Mqtt
{
    internal class Resources
    {
        internal static readonly string ByteExtensions_InvalidBitPosition = "Bit position must be between 0 and 7";

        internal static readonly string ByteExtensions_InvalidByteIndex = "Byte index must be from 1 to 8, starting from msb";

        internal static readonly string ConnectAckFormatter_InvalidAckFlags = "Bits 7-1 from Acknowledge flags are reserved and must be set to 0";

        internal static readonly string ConnectAckFormatter_InvalidSessionPresentForErrorReturnCode = "Session Present flag must be set to 0 for non-zero return codes";

        internal static readonly string ConnectFormatter_ClientIdMaxLengthExceeded = "Client Id cannot exceed 23 bytes";

        internal static readonly string ConnectFormatter_ClientIdRequired = "Client Id value is cannot be null or empty";

        internal static readonly string ConnectFormatter_InvalidClientIdFormat = "{0} is an invalid ClientId. It must contain only numbers and letters";

        internal static readonly string ConnectFormatter_InvalidPasswordFlag = "Password Flag must be set to 0 if the User Name Flag is set to 0";

        internal static readonly string ConnectFormatter_InvalidProtocolName = "{0} is not a valid protocol name";

        internal static readonly string ConnectFormatter_InvalidReservedFlag = "Reserved Flag must be always set to 0";

        internal static readonly string ConnectFormatter_InvalidWillRetainFlag = "Will Retain Flag must be set to 0 if the Will Flag is set to 0";

        internal static readonly string ConnectFormatter_PasswordNotAllowed = "Password value must be null or empty if User value is null or empty";

        internal static readonly string ConnectFormatter_UnsupportedLevel = "Protocol Level {0} is not supported by the server";

        internal static readonly string ProtocolEncoding_StringMaxLengthExceeded = "String value cannot exceed 65536 bytes of length";

        internal static readonly string Formatter_InvalidHeaderFlag = "Header Flag {0} is invalid for {1} packet. Expected value: {2}";

        internal static readonly string Formatter_InvalidPacket = "The packet sent cannot be handled by {0}";

        internal static readonly string Formatter_InvalidQualityOfService = "Qos value must be from 0x00 to 0x02";

        internal static readonly string PacketManager_PacketUnknown = "The received packet cannot be handled by any of the registered formatters";

        internal static readonly string ProtocolEncoding_MalformedRemainingLength = "Malformed Remaining Length";

        internal static readonly string ProtocolFlowProvider_UnknownPacketType = "An error occured while trying to get a Flow Type based on Packet Type {0}";

        internal static readonly string PublishFormatter_InvalidDuplicatedWithQoSZero = "Duplicated flag must be set to 0 if the QoS is 0";

        internal static readonly string PublishFormatter_InvalidPacketId = "Packet Id is not allowed for packets with QoS 0";

        internal static readonly string PublishFormatter_InvalidTopicName = "Topic name {0} is invalid. It cannot be null or empty and It must not contain wildcard characters";

        internal static readonly string PublishFormatter_PacketIdRequired = "Packet Id value cannot be null or empty for packets with QoS 1 or 2";

        internal static readonly string SubscribeAckFormatter_InvalidReturnCodes = "Return codes can only be valid QoS values or a failure code (0x80)";

        internal static readonly string SubscribeAckFormatter_MissingReturnCodes = "A subscribe acknowledge packet must contain at least one return code";

        internal static readonly string SubscribeFormatter_MissingTopicFilterQosPair = "A subscribe packet must contain at least one Topic Filter / QoS pair";

        internal static readonly string UnsubscribeFormatter_MissingTopics = "An unsubscribe packet must contain at least one topic to unsubscribe";

        internal static readonly string PublishReceiverFlow_PacketIdRequired = "Packet Id value is required for QoS major than 0";

        internal static readonly string SubscribeFormatter_InvalidTopicFilter = "Topic filter {0} is invalid. See protocol specification for more details on Topic Filter rules: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc398718106";

        internal static readonly string ProtocolEncoding_IntegerMaxValueExceeded = "Integer values are expected to be of two bytes length. The max value supported is 65536";

        internal static readonly string TopicEvaluator_InvalidTopicFilter = "The topic filter {0} is invalid according to the protocol rules and configuration";

        internal static readonly string TopicEvaluator_InvalidTopicName = "The topic name {0} is invalid according to the protocol rules";

        internal static readonly string PublishReceiverFlow_PacketIdNotAllowed = "Packet Id value is not allowed for QoS 0";

        internal static readonly string ProtocolFlowProvider_InvalidPacketType = "The packet type {0} cannot be handled by this flow provider";

        internal static readonly string PublishFlow_AckMonitor_ExceededMaximumAckRetries = "The QoS publish flow was not completed within the maximum configured message retries of {0}. The connection will be closed";

        internal static readonly string SessionRepository_ClientSessionNotFound = "No session has been found for client {0}";

        internal static readonly string Tracer_PublishFlow_RetryingQoSFlow = "The ack for message {0} has not been received. Re sending message for client {1}";

        internal static readonly string Tracer_Disposing = "Disposing {0}";

        internal static readonly string TcpChannel_ClientIsNotConnected = "The underlying TCP client is not connected";

        internal static readonly string TcpChannel_SocketDisconnected = "The underlying network stream is not available. The socket could became disconnected";

        internal static readonly string Tracer_TcpChannel_DisposeError = "An error occurred while closing underlying channel. Error code: {0}";

        internal static readonly string Tracer_TcpChannel_NetworkStreamCompleted = "The TCP Network Stream has completed sending bytes. The observable sequence will be completed and the channel will be disposed";

        internal static readonly string Tracer_TcpChannel_ReceivedPacket = "Received packet of {0} bytes";

        internal static readonly string Tracer_TcpChannel_SendingPacket = "Sending packet of {0} bytes";

        internal static readonly string TcpChannelFactory_TcpClient_Failed = "An error occurred while connecting via TCP to the endpoint address {0} and port {1}, to establish an MQTT connection";

        internal static readonly string PacketChannelFactory_InnerChannelFactoryNotFound = "An inner channel factory is required to create a new packet channel";

        internal static readonly string TcpChannelProvider_TcpListener_Failed = "An error occurred while starting to listen incoming TCP connections";

        internal static readonly string TcpChannel_DisposeError = "An error occurred while closing underlying channel. Error code: {0}";

        internal static readonly string TcpChannel_NetworkStreamCompleted = "The TCP Network Stream has completed sending bytes. The observable sequence will be completed and the channel will be disposed";

        internal static readonly string TcpChannel_ReceivedPacket = "Received packet of {0} bytes";

        internal static readonly string TcpChannel_SendingPacket = "Sending packet of {0} bytes";

        internal static readonly string ClientPacketListener_FirstReceivedPacketMustBeConnectAck = "The first packet received from the Server must be a ConnectAck packet. The connection will be closed.";

        internal static readonly string Client_ConnectionDisconnected = "The client {0} has been disconnected while trying to perform the connection";

        internal static readonly string Client_ConnectionError = "An error occurred while trying to connect the client {0}";

        internal static readonly string Client_ConnectionTimeout = "A timeout occured while waiting for the client {0} connection confirmation";

        internal static readonly string Client_ConnectNotAccepted = "The connect packet of client {0} has not been accepted by the server. Status: {1}. The connection will be closed";

        internal static readonly string Client_SubscribeError = "An error occurred while trying to subscribe the client {0} to topic {1}";

        internal static readonly string Client_SubscribeTimeout = "A timeout occured while waiting for the subscribe confirmation of client {0} for topic {1}";

        internal static readonly string Client_SubscriptionDisconnected = "The client {0} has been disconnected while trying to perform the subscription to topic {1}";

        internal static readonly string Client_UnexpectedChannelDisconnection = "The underlying connection has been disconnected unexpectedly";

        internal static readonly string Client_UnsubscribeDisconnected = "The client {0} has been disconnected while trying to perform the unsubscribe to topics: {1}";

        internal static readonly string Client_UnsubscribeError = "An error occurred while trying to unsubscribe the client {0} of topics: {1}";

        internal static readonly string Client_UnsubscribeTimeout = "A timeout occured while waiting for the unsubscribe confirmation of client {0} for topics: {1}";

        internal static readonly string Tracer_ClientPacketListener_DispatchingMessage = "Client {0} - Dispatching {1} message to flow {2}";

        internal static readonly string Tracer_ClientPacketListener_DispatchingPublish = "Client {0} - Dispatching Publish message to flow {1} and topic {2}";

        internal static readonly string Tracer_ClientPacketListener_Error = "Client - An error occurred while listening and dispatching packets";

        internal static readonly string Tracer_ClientPacketListener_FirstPacketReceived = "Client {0} - First packet from Server has been received. Type: {1}";

        internal static readonly string Tracer_ClientPacketListener_SendingKeepAlive = "Client {0} - No packet has been sent in {1} seconds. Sending Ping to Server to maintain Keep Alive";

        internal static readonly string Tracer_Client_CleanedOldSession = "Client {0} - Cleaned old session";

        internal static readonly string Tracer_Client_CreatedSession = "Client {0} - Created new client session";

        internal static readonly string Tracer_Client_DeletedSessionOnDisconnect = "Client {0} - Removed client session as part of Disconnect";

        internal static readonly string Tracer_Client_Disposing = "Client {0} - Disposing. Reason: {1}";

        internal static readonly string Tracer_Client_PacketsObservableCompleted = "Client - Packet observable sequence has been completed, hence closing the channel";

        internal static readonly string Tracer_NewApplicationMessageReceived = "Client {0} - An application message for topic {1} was received";

        internal static readonly string Tracer_PacketChannelCompleted = "Client {0} - Packet Channel observable sequence has been completed";

        internal static readonly string Client_InitializeError = "An error occurred while initializing a client";
    }
}
