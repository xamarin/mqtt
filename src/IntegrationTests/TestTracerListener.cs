using System.Diagnostics;

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
				
			Debug.WriteLine (GetTestLogMessage (eventCache, eventType, message));
		}

		string GetTestLogMessage(TraceEventCache eventCache, TraceEventType eventType, string message)
		{
			return string.Format ("Thread {0} - {1} - {2} - {3}", eventCache.ThreadId.PadLeft(4), 
				eventCache.DateTime.ToString("MM/dd/yyyy hh:mm:ss.fff").PadLeft(4), eventType, message);
		}
	}
}
