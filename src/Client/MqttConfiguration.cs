namespace System.Net.Mqtt
{
	/// <summary>
	/// General configuration used across the protocol implementation
	/// </summary>
	public class MqttConfiguration
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttConfiguration" /> class 
        /// </summary>
		public MqttConfiguration ()
		{
			Port = MqttProtocol.DefaultNonSecurePort;
			// The default receive buffer size of TcpClient according to
			// http://msdn.microsoft.com/en-us/library/system.net.sockets.tcpclient.receivebuffersize.aspx
			// is 8192 bytes
			BufferSize = 8192;
			MaximumQualityOfService = MqttQualityOfService.AtMostOnce;
			KeepAliveSecs = 0;
			WaitTimeoutSecs = 5;
			ConnectionTimeoutSecs = 5;
			AllowWildcardsInTopicFilters = true;
		}

        /// <summary>
        /// Port to connect a Client to a Server
        /// </summary>
		public int Port { get; set; }

        /// <summary>
        /// Size in bytes of the receive buffer of the underlying transport protocol
        /// Only use it when the property applies to the undelrying protocol used
        /// </summary>
		public int BufferSize { get; set; }

        /// <summary>
        /// Maximum Quality of Service (QoS) to support
        /// Default value is AtMostOnce, which means QoS 0
        /// </summary>
		public MqttQualityOfService MaximumQualityOfService { get; set; }

        /// <summary>
        /// Seconds to wait for the MQTT Keep Alive mechanism
        /// until a Ping packet is sent to maintain the connection alive
        /// Default value is 0 seconds, which means Keep Alive disabled
        /// </summary>
		public ushort KeepAliveSecs { get; set; }

        /// <summary>
        /// Seconds to wait for an incoming required message until the operation timeouts
        /// This value is generally used to wait for Server or Client acknowledgements
        /// Default value is 5 seconds
        /// </summary>
		public int WaitTimeoutSecs { get; set; }

        /// <summary>
        /// Seconds to wait for a channel connection until the operation timeouts.
        /// This value is generally used to wait for the MQTT channel to open.
        /// Default value is 5 seconds
        /// </summary>
		public int ConnectionTimeoutSecs { get; set; }

        /// <summary>
        /// Determines if multi level (#)  and single level (+)
        /// wildcards are allowed on topic filters
        /// Default value is true
        /// </summary>
		public bool AllowWildcardsInTopicFilters { get; set; }
	}
}
