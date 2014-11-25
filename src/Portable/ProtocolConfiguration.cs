using Hermes.Packets;

namespace Hermes
{
	public class ProtocolConfiguration
	{
		public ProtocolConfiguration ()
		{
			this.AllowWildcardsInTopicFilters = true;
		}

		public int Port { get; set; }

		public QualityOfService MaximumQualityOfService { get; set; }

		public ushort KeepAliveSecs { get; set; }

		public int WaitingTimeoutSecs { get; set; }

		public bool AllowWildcardsInTopicFilters { get; set; }
	}
}
