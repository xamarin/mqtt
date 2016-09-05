﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace System.Net.Mqtt.Properties {
    using System;
    using System.Reflection;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("System.Net.Mqtt.Properties.Resources", typeof(Resources).GetTypeInfo().Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Bit position must be between 0 and 7.
        /// </summary>
        internal static string ByteExtensions_InvalidBitPosition {
            get {
                return ResourceManager.GetString("ByteExtensions_InvalidBitPosition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Byte index must be from 1 to 8, starting from msb.
        /// </summary>
        internal static string ByteExtensions_InvalidByteIndex {
            get {
                return ResourceManager.GetString("ByteExtensions_InvalidByteIndex", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client {0} - Cleaned old session.
        /// </summary>
        internal static string Client_CleanedOldSession {
            get {
                return ResourceManager.GetString("Client_CleanedOldSession", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The client {0} has been disconnected while trying to perform the connection.
        /// </summary>
        internal static string Client_ConnectionDisconnected {
            get {
                return ResourceManager.GetString("Client_ConnectionDisconnected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while trying to connect the client {0}.
        /// </summary>
        internal static string Client_ConnectionError {
            get {
                return ResourceManager.GetString("Client_ConnectionError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A timeout occured while waiting for the client {0} connection confirmation.
        /// </summary>
        internal static string Client_ConnectionTimeout {
            get {
                return ResourceManager.GetString("Client_ConnectionTimeout", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The connect packet of client {0} has not been accepted by the server. Status: {1}. The connection will be closed.
        /// </summary>
        internal static string Client_ConnectNotAccepted {
            get {
                return ResourceManager.GetString("Client_ConnectNotAccepted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client {0} - Created new client session.
        /// </summary>
        internal static string Client_CreatedSession {
            get {
                return ResourceManager.GetString("Client_CreatedSession", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client {0} - Removed client session as part of Disconnect.
        /// </summary>
        internal static string Client_DeletedSessionOnDisconnect {
            get {
                return ResourceManager.GetString("Client_DeletedSessionOnDisconnect", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client {0} - Disposing. Reason: {1}.
        /// </summary>
        internal static string Client_Disposing {
            get {
                return ResourceManager.GetString("Client_Disposing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while initializing a client.
        /// </summary>
        internal static string Client_InitializeError {
            get {
                return ResourceManager.GetString("Client_InitializeError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client {0} - An application message for topic {1} was received.
        /// </summary>
        internal static string Client_NewApplicationMessageReceived {
            get {
                return ResourceManager.GetString("Client_NewApplicationMessageReceived", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client - Packet observable sequence has been completed, hence closing the channel.
        /// </summary>
        internal static string Client_PacketsObservableCompleted {
            get {
                return ResourceManager.GetString("Client_PacketsObservableCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while trying to subscribe the client {0} to topic {1}.
        /// </summary>
        internal static string Client_SubscribeError {
            get {
                return ResourceManager.GetString("Client_SubscribeError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A timeout occured while waiting for the subscribe confirmation of client {0} for topic {1}.
        /// </summary>
        internal static string Client_SubscribeTimeout {
            get {
                return ResourceManager.GetString("Client_SubscribeTimeout", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The client {0} has been disconnected while trying to perform the subscription to topic {1}.
        /// </summary>
        internal static string Client_SubscriptionDisconnected {
            get {
                return ResourceManager.GetString("Client_SubscriptionDisconnected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The underlying connection has been disconnected unexpectedly.
        /// </summary>
        internal static string Client_UnexpectedChannelDisconnection {
            get {
                return ResourceManager.GetString("Client_UnexpectedChannelDisconnection", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The client {0} has been disconnected while trying to perform the unsubscribe to topics: {1}.
        /// </summary>
        internal static string Client_UnsubscribeDisconnected {
            get {
                return ResourceManager.GetString("Client_UnsubscribeDisconnected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while trying to unsubscribe the client {0} of topics: {1}.
        /// </summary>
        internal static string Client_UnsubscribeError {
            get {
                return ResourceManager.GetString("Client_UnsubscribeError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A timeout occured while waiting for the unsubscribe confirmation of client {0} for topics: {1}.
        /// </summary>
        internal static string Client_UnsubscribeTimeout {
            get {
                return ResourceManager.GetString("Client_UnsubscribeTimeout", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client {0} - Dispatching {1} message to flow {2}.
        /// </summary>
        internal static string ClientPacketListener_DispatchingMessage {
            get {
                return ResourceManager.GetString("ClientPacketListener_DispatchingMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client {0} - Dispatching Publish message to flow {1} and topic {2}.
        /// </summary>
        internal static string ClientPacketListener_DispatchingPublish {
            get {
                return ResourceManager.GetString("ClientPacketListener_DispatchingPublish", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client - An error occurred while listening and dispatching packets.
        /// </summary>
        internal static string ClientPacketListener_Error {
            get {
                return ResourceManager.GetString("ClientPacketListener_Error", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client {0} - First packet from Server has been received. Type: {1}.
        /// </summary>
        internal static string ClientPacketListener_FirstPacketReceived {
            get {
                return ResourceManager.GetString("ClientPacketListener_FirstPacketReceived", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The first packet received from the Server must be a ConnectAck packet. The connection will be closed..
        /// </summary>
        internal static string ClientPacketListener_FirstReceivedPacketMustBeConnectAck {
            get {
                return ResourceManager.GetString("ClientPacketListener_FirstReceivedPacketMustBeConnectAck", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client {0} - Packet Channel observable sequence has been completed.
        /// </summary>
        internal static string ClientPacketListener_PacketChannelCompleted {
            get {
                return ResourceManager.GetString("ClientPacketListener_PacketChannelCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client {0} - No packet has been sent in {1} seconds. Sending Ping to Server to maintain Keep Alive.
        /// </summary>
        internal static string ClientPacketListener_SendingKeepAlive {
            get {
                return ResourceManager.GetString("ClientPacketListener_SendingKeepAlive", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Bits 7-1 from Acknowledge flags are reserved and must be set to 0.
        /// </summary>
        internal static string ConnectAckFormatter_InvalidAckFlags {
            get {
                return ResourceManager.GetString("ConnectAckFormatter_InvalidAckFlags", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Session Present flag must be set to 0 for non-zero return codes.
        /// </summary>
        internal static string ConnectAckFormatter_InvalidSessionPresentForErrorReturnCode {
            get {
                return ResourceManager.GetString("ConnectAckFormatter_InvalidSessionPresentForErrorReturnCode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client Id cannot exceed 23 bytes.
        /// </summary>
        internal static string ConnectFormatter_ClientIdMaxLengthExceeded {
            get {
                return ResourceManager.GetString("ConnectFormatter_ClientIdMaxLengthExceeded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Client Id value is cannot be null or empty.
        /// </summary>
        internal static string ConnectFormatter_ClientIdRequired {
            get {
                return ResourceManager.GetString("ConnectFormatter_ClientIdRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is an invalid ClientId. It must contain only numbers and letters.
        /// </summary>
        internal static string ConnectFormatter_InvalidClientIdFormat {
            get {
                return ResourceManager.GetString("ConnectFormatter_InvalidClientIdFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Password Flag must be set to 0 if the User Name Flag is set to 0.
        /// </summary>
        internal static string ConnectFormatter_InvalidPasswordFlag {
            get {
                return ResourceManager.GetString("ConnectFormatter_InvalidPasswordFlag", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is not a valid protocol name.
        /// </summary>
        internal static string ConnectFormatter_InvalidProtocolName {
            get {
                return ResourceManager.GetString("ConnectFormatter_InvalidProtocolName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Reserved Flag must be always set to 0.
        /// </summary>
        internal static string ConnectFormatter_InvalidReservedFlag {
            get {
                return ResourceManager.GetString("ConnectFormatter_InvalidReservedFlag", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Will Retain Flag must be set to 0 if the Will Flag is set to 0.
        /// </summary>
        internal static string ConnectFormatter_InvalidWillRetainFlag {
            get {
                return ResourceManager.GetString("ConnectFormatter_InvalidWillRetainFlag", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Password value must be null or empty if User value is null or empty.
        /// </summary>
        internal static string ConnectFormatter_PasswordNotAllowed {
            get {
                return ResourceManager.GetString("ConnectFormatter_PasswordNotAllowed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Protocol Level {0} is not supported by the server.
        /// </summary>
        internal static string ConnectFormatter_UnsupportedLevel {
            get {
                return ResourceManager.GetString("ConnectFormatter_UnsupportedLevel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Header Flag {0} is invalid for {1} packet. Expected value: {2}.
        /// </summary>
        internal static string Formatter_InvalidHeaderFlag {
            get {
                return ResourceManager.GetString("Formatter_InvalidHeaderFlag", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The packet sent cannot be handled by {0}.
        /// </summary>
        internal static string Formatter_InvalidPacket {
            get {
                return ResourceManager.GetString("Formatter_InvalidPacket", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Qos value must be from 0x00 to 0x02.
        /// </summary>
        internal static string Formatter_InvalidQualityOfService {
            get {
                return ResourceManager.GetString("Formatter_InvalidQualityOfService", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Disposing {0}....
        /// </summary>
        internal static string Mqtt_Disposing {
            get {
                return ResourceManager.GetString("Mqtt_Disposing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The underlying communication stream is not connected.
        /// </summary>
        internal static string MqttChannel_ClientNotConnected {
            get {
                return ResourceManager.GetString("MqttChannel_ClientNotConnected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while closing underlying communication channel. Error code: {0}.
        /// </summary>
        internal static string MqttChannel_DisposeError {
            get {
                return ResourceManager.GetString("MqttChannel_DisposeError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The underlying communication stream has completed sending bytes. The observable sequence will be completed and the channel will be disposed.
        /// </summary>
        internal static string MqttChannel_NetworkStreamCompleted {
            get {
                return ResourceManager.GetString("MqttChannel_NetworkStreamCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Received packet of {0} bytes.
        /// </summary>
        internal static string MqttChannel_ReceivedPacket {
            get {
                return ResourceManager.GetString("MqttChannel_ReceivedPacket", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sending packet of {0} bytes.
        /// </summary>
        internal static string MqttChannel_SendingPacket {
            get {
                return ResourceManager.GetString("MqttChannel_SendingPacket", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The underlying communication stream is not available. The socket could became disconnected.
        /// </summary>
        internal static string MqttChannel_StreamDisconnected {
            get {
                return ResourceManager.GetString("MqttChannel_StreamDisconnected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An inner channel factory is required to create a new packet channel.
        /// </summary>
        internal static string PacketChannelFactory_InnerChannelFactoryNotFound {
            get {
                return ResourceManager.GetString("PacketChannelFactory_InnerChannelFactoryNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The received packet cannot be handled by any of the registered formatters.
        /// </summary>
        internal static string PacketManager_PacketUnknown {
            get {
                return ResourceManager.GetString("PacketManager_PacketUnknown", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Integer values are expected to be of two bytes length. The max value supported is 65536.
        /// </summary>
        internal static string ProtocolEncoding_IntegerMaxValueExceeded {
            get {
                return ResourceManager.GetString("ProtocolEncoding_IntegerMaxValueExceeded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Malformed Remaining Length.
        /// </summary>
        internal static string ProtocolEncoding_MalformedRemainingLength {
            get {
                return ResourceManager.GetString("ProtocolEncoding_MalformedRemainingLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to String value cannot exceed 65536 bytes of length.
        /// </summary>
        internal static string ProtocolEncoding_StringMaxLengthExceeded {
            get {
                return ResourceManager.GetString("ProtocolEncoding_StringMaxLengthExceeded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The packet type {0} cannot be handled by this flow provider.
        /// </summary>
        internal static string ProtocolFlowProvider_InvalidPacketType {
            get {
                return ResourceManager.GetString("ProtocolFlowProvider_InvalidPacketType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occured while trying to get a Flow Type based on Packet Type {0}.
        /// </summary>
        internal static string ProtocolFlowProvider_UnknownPacketType {
            get {
                return ResourceManager.GetString("ProtocolFlowProvider_UnknownPacketType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The ack for message {0} has not been received. Re sending message for client {1}.
        /// </summary>
        internal static string PublishFlow_RetryingQoSFlow {
            get {
                return ResourceManager.GetString("PublishFlow_RetryingQoSFlow", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Duplicated flag must be set to 0 if the QoS is 0.
        /// </summary>
        internal static string PublishFormatter_InvalidDuplicatedWithQoSZero {
            get {
                return ResourceManager.GetString("PublishFormatter_InvalidDuplicatedWithQoSZero", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Packet Id is not allowed for packets with QoS 0.
        /// </summary>
        internal static string PublishFormatter_InvalidPacketId {
            get {
                return ResourceManager.GetString("PublishFormatter_InvalidPacketId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Topic name {0} is invalid. It cannot be null or empty and It must not contain wildcard characters.
        /// </summary>
        internal static string PublishFormatter_InvalidTopicName {
            get {
                return ResourceManager.GetString("PublishFormatter_InvalidTopicName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Packet Id value cannot be null or empty for packets with QoS 1 or 2.
        /// </summary>
        internal static string PublishFormatter_PacketIdRequired {
            get {
                return ResourceManager.GetString("PublishFormatter_PacketIdRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Packet Id value is not allowed for QoS 0.
        /// </summary>
        internal static string PublishReceiverFlow_PacketIdNotAllowed {
            get {
                return ResourceManager.GetString("PublishReceiverFlow_PacketIdNotAllowed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Packet Id value is required for QoS major than 0.
        /// </summary>
        internal static string PublishReceiverFlow_PacketIdRequired {
            get {
                return ResourceManager.GetString("PublishReceiverFlow_PacketIdRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No session has been found for client {0}.
        /// </summary>
        internal static string SessionRepository_ClientSessionNotFound {
            get {
                return ResourceManager.GetString("SessionRepository_ClientSessionNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Return codes can only be valid QoS values or a failure code (0x80).
        /// </summary>
        internal static string SubscribeAckFormatter_InvalidReturnCodes {
            get {
                return ResourceManager.GetString("SubscribeAckFormatter_InvalidReturnCodes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A subscribe acknowledge packet must contain at least one return code.
        /// </summary>
        internal static string SubscribeAckFormatter_MissingReturnCodes {
            get {
                return ResourceManager.GetString("SubscribeAckFormatter_MissingReturnCodes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Topic filter {0} is invalid. See protocol specification for more details on Topic Filter rules: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc398718106.
        /// </summary>
        internal static string SubscribeFormatter_InvalidTopicFilter {
            get {
                return ResourceManager.GetString("SubscribeFormatter_InvalidTopicFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A subscribe packet must contain at least one Topic Filter / QoS pair.
        /// </summary>
        internal static string SubscribeFormatter_MissingTopicFilterQosPair {
            get {
                return ResourceManager.GetString("SubscribeFormatter_MissingTopicFilterQosPair", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while connecting via TCP to the endpoint address {0} and port {1}, to establish an MQTT connection.
        /// </summary>
        internal static string TcpChannelFactory_TcpClient_Failed {
            get {
                return ResourceManager.GetString("TcpChannelFactory_TcpClient_Failed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while starting to listen incoming TCP connections.
        /// </summary>
        internal static string TcpChannelProvider_TcpListener_Failed {
            get {
                return ResourceManager.GetString("TcpChannelProvider_TcpListener_Failed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The topic filter {0} is invalid according to the protocol rules and configuration.
        /// </summary>
        internal static string TopicEvaluator_InvalidTopicFilter {
            get {
                return ResourceManager.GetString("TopicEvaluator_InvalidTopicFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The topic name {0} is invalid according to the protocol rules.
        /// </summary>
        internal static string TopicEvaluator_InvalidTopicName {
            get {
                return ResourceManager.GetString("TopicEvaluator_InvalidTopicName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An unsubscribe packet must contain at least one topic to unsubscribe.
        /// </summary>
        internal static string UnsubscribeFormatter_MissingTopics {
            get {
                return ResourceManager.GetString("UnsubscribeFormatter_MissingTopics", resourceCulture);
            }
        }
    }
}
