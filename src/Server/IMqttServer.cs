using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents an MQTT Server
    /// </summary>
    public interface IMqttServer : IDisposable
    {
        /// <summary>
        /// Event raised when a message published by a Client 
        /// has no subscribers for the published topic
        /// See <see cref="MqttUndeliveredMessage" /> for more details 
        /// about the information exposed by this event 
        /// </summary>
        event EventHandler<MqttUndeliveredMessage> MessageUndelivered;

        /// <summary>
        /// Event fired when the Server gets stopped.
        /// The Server disconnection could be caused by an intentional Stop or disposal, 
        /// or an error during a Server operation
        /// See <see cref="MqttEndpointDisconnected"/> for more details on the disconnection information
        /// </summary>
        event EventHandler<MqttEndpointDisconnected> Stopped;

		/// <summary>
		/// Event raised when a client has successfully authenticated and connected to the server.
		/// </summary>
		event EventHandler<string> ClientConnected;

		/// <summary>
		/// Event raised when a client has been disconnected from the server.
		/// </summary>
		event EventHandler<string> ClientDisconnected;

        /// <summary>
        /// Gets the current number of active connections to the Server.
        /// </summary>
		/// <remarks>
		/// <see cref="ActiveConnections"/> may temporarily differ with <see cref="ActiveClients"/> 
		/// since the latter represents channels that have performed the MQTT Connect flow and 
		/// have provided a valid Client ID and have been successfully authenticated (if authentication 
		/// is supported by the server).
		/// </remarks>
        int ActiveConnections { get; }

        /// <summary>
        /// Gets the list of Client Ids connected to the Server, once they have performed the 
		/// Connect flow over their Channel and they have been authenticated.
        /// </summary>
        IEnumerable<string> ActiveClients { get; }

        /// <summary>
        /// Starts the Server and enables it to listen for incoming connections
        /// </summary>
        /// <exception cref="MqttException">MqttException</exception>
        void Start ();

        /// <summary>
        /// Creates an in process client and establishes the protocol 
        /// connection before returning it to the caller
        /// See <see cref="IMqttConnectedClient" /> for more details about in process clients 
        /// </summary>
        /// <returns>Returns a connected client ready to use</returns>
        Task<IMqttConnectedClient> CreateClientAsync ();

        /// <summary>
        /// Stops the server and disposes it.
        /// </summary>
        void Stop ();
    }
}
