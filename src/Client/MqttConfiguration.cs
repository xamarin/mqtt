using System.Net.Mqtt.Packets;

namespace System.Net.Mqtt
{
	public class MqttConfiguration
	{
		public MqttConfiguration ()
		{
			Port = MqttProtocol.DefaultNonSecurePort;
			// The default receive buffer size of TcpClient according to
			// http://msdn.microsoft.com/en-us/library/system.net.sockets.tcpclient.receivebuffersize.aspx
			// is 8192 bytes
			BufferSize = 8192;
			MaximumQualityOfService = MqttQualityOfService.AtMostOnce;
			KeepAliveSecs = 0;
			WaitingTimeoutSecs = 5;
			AllowWildcardsInTopicFilters = true;
		}

		public int Port { get; set; }

		public int BufferSize { get; set; }

		public MqttQualityOfService MaximumQualityOfService { get; set; }

		public ushort KeepAliveSecs { get; set; }

		public int WaitingTimeoutSecs { get; set; }

		public bool AllowWildcardsInTopicFilters { get; set; }
	}
}
