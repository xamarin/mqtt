namespace System.Net.Mqtt.Client
{
    internal class Resources
    {
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

        internal static readonly string SessionRepository_ClientSessionNotFound = "No session has been found for client {0}";

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

        internal static readonly string Tracer_Disposing = "Disposing {0}";

        internal static readonly string Tracer_NewApplicationMessageReceived = "Client {0} - An application message for topic {1} was received";

        internal static readonly string Tracer_PacketChannelCompleted = "Client {0} - Packet Channel observable sequence has been completed";

        internal static readonly string Client_InitializeError = "An error occurred while initializing a client";

        internal static readonly string TcpChannelFactory_TcpClient_Failed = "An error occurred while connecting via TCP to the endpoint address {0} and port {1}, to establish an MQTT connection";

        internal static readonly string TcpChannelProvider_TcpListener_Failed = "An error occurred while starting to listen incoming TCP connections";

        internal static readonly string TcpChannel_ClientIsNotConnected = "The underlying TCP client is not connected";

        internal static readonly string TcpChannel_DisposeError = "An error occurred while closing underlying channel. Error code: {0}";

        internal static readonly string TcpChannel_NetworkStreamCompleted = "The TCP Network Stream has completed sending bytes. The observable sequence will be completed and the channel will be disposed";

        internal static readonly string TcpChannel_ReceivedPacket = "Received packet of {0} bytes";

        internal static readonly string TcpChannel_SendingPacket = "Sending packet of {0} bytes";

        internal static readonly string TcpChannel_SocketDisconnected = "The underlying network stream is not available. The socket could became disconnected";
    }
}
