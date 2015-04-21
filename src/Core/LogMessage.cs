using System;
using System.Threading;

namespace Hermes
{
	public class LogMessage
	{
		public static string Create(string message, params object[] parameters)
		{
			var formattedMessage = string.Format(message, parameters);

			return string.Format ("Thread {0} - {1} - {2}", Thread.CurrentThread.ManagedThreadId, DateTime.Now.ToString ("MM/dd/yyyy hh:mm:ss.fff"), formattedMessage);
		}
	}
}
