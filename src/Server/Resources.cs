namespace System.Net.Mqtt.Server
{
    public class Resources
    {
        internal static readonly string TcpChannelProvider_TcpListener_Failed = "An error occurred while starting to listen incoming TCP connections";

        internal static readonly string ServerPacketListener_FirstPacketMustBeConnect = "The first packet sent by a Client must be a Connect. The connection will be closed.";

        internal static readonly string ServerPacketListener_KeepAliveTimeExceeded = "The keep alive tolerance of {0} seconds has been exceeded and no packet has been received from client {1}. The connection will be closed.";

        internal static readonly string ServerPacketListener_NoConnectReceived = "No connect packet has been received since the network connection was established. The connection will be closed.";

        internal static readonly string ServerPacketListener_SecondConnectNotAllowed = "Only one Connect packet is allowed. The connection will be closed.";

        internal static readonly string SessionRepository_ClientSessionNotFound = "No session has been found for client {0}";

        internal static readonly string Tracer_ConnectionProvider_ClientDisconnected = "Server - The connection for client {0} is not connected. Removing connection";

        internal static readonly string Tracer_ConnectionProvider_ClientIdExists = "An active connection already exists for client {0}. Disposing current connection and adding the new one";

        internal static readonly string Tracer_ConnectionProvider_RemovingClient = "Server - Removing connection of client {0}";

        internal static readonly string Tracer_DisconnectFlow_Disconnecting = "Server - Disconnecting client {0}";

        internal static readonly string Tracer_Disposing = "Disposing {0}";

        internal static readonly string Tracer_PacketChannelCompleted = "Server - Packet Channel observable sequence has been completed for client {0}";

        internal static readonly string Tracer_ServerPacketListener_ConnectionError = "Server - An error occurred while executing the connect flow. Client: {0}";

        internal static readonly string Tracer_ServerPacketListener_ConnectPacketReceived = "Server - A connect packet has been received from client {0}";

        internal static readonly string Tracer_ServerPacketListener_DispatchingMessage = "Server - Dispatching {0} message to flow {1} for client {2}";

        internal static readonly string Tracer_ServerPacketListener_DispatchingPublish = "Server - Dispatching Publish message to flow {0} for client {1} and topic {2}";

        internal static readonly string Tracer_ServerPacketListener_DispatchingSubscribe = "Server - Dispatching Subscribe message to flow {0} for client {1} and topics: {2}";

        internal static readonly string Tracer_ServerPacketListener_Error = "Server - An error occurred while listening and dispatching packets - Client: {0}";

        internal static readonly string Tracer_ServerPublishReceiverFlow_SendingWill = "Server - Sending last will message of client {0} to topic {1}";

        internal static readonly string Tracer_ServerPublishReceiverFlow_TopicNotSubscribed = "The topic {0} has no subscribers, hence the message sent by {1} will not be forwarded";

        internal static readonly string Tracer_ServerSubscribeFlow_ErrorOnSubscription = "Server - An error occurred when subscribing client {0} to topic {1}";

        internal static readonly string Tracer_ServerSubscribeFlow_InvalidTopicSubscription = "Server - The topic {0}, sent by client {1} is invalid. Returning failure code";

        internal static readonly string Tracer_Server_CleanedOldSession = "Server - Cleaned old session for client {0}";

        internal static readonly string Tracer_Server_CreatedSession = "Server - Created new session for client {0}";

        internal static readonly string Tracer_Server_DeletedSessionOnDisconnect = "Server - Removed session for client {0} as part of Disconnect flow";

        internal static readonly string Tracer_Server_NewSocketAccepted = "Server - A new TCP channel has been accepted";

        internal static readonly string Tracer_Server_PacketsObservableCompleted = "Server - Packet observable sequence has been completed, hence closing the channel";

        internal static readonly string Tracer_Server_PacketsObservableError = "Server - Packet observable sequence had an error, hence closing the channel";

        internal static readonly string Server_InitializeError = "An error occurred while initializing the server";
    }
}
