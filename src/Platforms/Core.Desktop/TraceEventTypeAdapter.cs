namespace System.Net.Mqtt
{
	internal class TraceEventTypeAdapter
	{
		internal System.Diagnostics.TraceEventType Get (Diagnostics.TraceEventType type)
		{
			switch (type) {
				case Diagnostics.TraceEventType.Critical:
					return System.Diagnostics.TraceEventType.Critical;
				case Diagnostics.TraceEventType.Error:
					return System.Diagnostics.TraceEventType.Error;
				case Diagnostics.TraceEventType.Warning:
					return System.Diagnostics.TraceEventType.Warning;
				case Diagnostics.TraceEventType.Information:
					return System.Diagnostics.TraceEventType.Information;
				case Diagnostics.TraceEventType.Verbose:
					return System.Diagnostics.TraceEventType.Verbose;
				case Diagnostics.TraceEventType.Start:
					return System.Diagnostics.TraceEventType.Start;
				case Diagnostics.TraceEventType.Stop:
					return System.Diagnostics.TraceEventType.Stop;
				case Diagnostics.TraceEventType.Suspend:
					return System.Diagnostics.TraceEventType.Suspend;
				case Diagnostics.TraceEventType.Resume:
					return System.Diagnostics.TraceEventType.Resume;
				case Diagnostics.TraceEventType.Transfer:
					return System.Diagnostics.TraceEventType.Transfer;
				default:
					return System.Diagnostics.TraceEventType.Information;
			}
		}
	}
}
