using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace IntegrationTests
{
	public class HermesTraceListener : TraceListener
	{
		private static readonly string logFolder = "Logs";
		private static readonly string logFile = @"hermes.log";
		private static readonly string errorFile = @"hermes.err";

		static HermesTraceListener()
		{
			if (!Directory.Exists (logFolder)) {
				Directory.CreateDirectory (logFolder);
			}
		}

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

			var logMessage = this.GetTestLogMessage (eventCache, message);

			if (eventType == TraceEventType.Error || eventType == TraceEventType.Critical) {
				File.AppendAllLines (this.GetErrorFile(), new List<string> { logMessage });
			} else {
				File.AppendAllLines (this.GetLogFile(), new List<string> { logMessage });
			}
			
			Console.WriteLine (logMessage);
		}

		private string GetLogFile()
		{
			return Path.Combine (logFolder, logFile);
		}

		private string GetErrorFile()
		{
			return Path.Combine (logFolder, errorFile);
		}

		private string GetTestLogMessage(TraceEventCache eventCache, string message)
		{
			return string.Format ("Thread {0} - {1} - {2}", eventCache.ThreadId.PadLeft(4), 
				(TimeSpan.FromTicks(eventCache.Timestamp).Seconds * 1000 + TimeSpan.FromTicks(eventCache.Timestamp).Milliseconds)
				.ToString().PadLeft(4), message);
		}
	}
}
