using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Net.Mqtt
{
    /// <summary>
    /// Represents an MQTT Broker
    /// </summary>
    public interface IMqttServer : IDisposable
    {
        /// <summary>
        /// Event fired when a message published by a Client 
        /// has no subscribers for the published topic
        /// See <see cref="MqttUndeliveredMessage" /> for more details 
        /// about the information exposed by this event 
        /// </summary>
        event EventHandler<MqttUndeliveredMessage> MessageUndelivered;

        /// <summary>
        /// Event fired when the Broker gets stopped.
        /// The Broker disconnection could be caused by an intentional Stop or disposal, 
        /// or an error during a Broker operation
        /// See <see cref="MqttEndpointDisconnected"/> for more details on the disconnection information
        /// </summary>
        event EventHandler<MqttEndpointDisconnected> Stopped;

        /// <summary>
        /// Gets the current number of active channels connected to the Broker
        /// </summary>
        int ActiveChannels { get; }

        /// <summary>
        /// Gets the list of Client Ids connected to the Broker
        /// </summary>
        IEnumerable<string> ActiveClients { get; }

        /// <summary>
        /// Starts the Broker and enables it to listen for incoming connections
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
        /// Stops the server and disposes it
        /// </summary>
        void Stop ();
    }
}
