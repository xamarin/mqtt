using System;

namespace Hermes
{
	public enum StoppedReason
	{
		Disconnect,
		Dispose,
		Error
	}

	public class StoppedEventArgs : EventArgs
	{
		public StoppedEventArgs (StoppedReason reason, string message = null)
		{
			this.Reason = reason;
			this.Message = message;
		}

		public StoppedReason Reason { get; private set; }

		public string Message { get; private set; }
	}
}
