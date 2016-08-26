using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	public class ProtocolConfiguration
	{
		public ProtocolConfiguration ()
		{
			Port = Protocol.DefaultNonSecurePort;
			// The default receive buffer size of TcpClient according to
			// http://msdn.microsoft.com/en-us/library/system.net.sockets.tcpclient.receivebuffersize.aspx
			// is 8192 bytes
			BufferSize = 8192;
			MaximumQualityOfService = QualityOfService.AtMostOnce;
			KeepAliveSecs = 0;
			WaitingTimeoutSecs = 5;
			AllowWildcardsInTopicFilters = true;
		}

		public int Port { get; set; }

		public int BufferSize { get; set; }

		public QualityOfService MaximumQualityOfService { get; set; }

		public ushort KeepAliveSecs { get; set; }

		public int WaitingTimeoutSecs { get; set; }

		public bool AllowWildcardsInTopicFilters { get; set; }
	}
}
