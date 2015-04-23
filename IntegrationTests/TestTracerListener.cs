using System;
using System.Diagnostics;
using System.Threading;

namespace IntegrationTests
{
	public class TestTracerListener : TraceListener
	{
		public override void Write (string message)
		{
		}

		public override void WriteLine (string message)
		{
		}

		public override void TraceEvent (TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
		{
			var message = format;

			if (args != null) {
				message = string.Format (format, args);
			}
				
			Debug.WriteLine (this.GetTestLogMessage (eventCache, message));
		}

		private string GetTestLogMessage(TraceEventCache eventCache, string message)
		{
			return string.Format ("Thread {0} - {1} - {2}", eventCache.ThreadId.PadLeft(4), 
				(TimeSpan.FromTicks(eventCache.Timestamp).Seconds * 1000 + TimeSpan.FromTicks(eventCache.Timestamp).Milliseconds)
				.ToString().PadLeft(4), message);
		}
	}
}
