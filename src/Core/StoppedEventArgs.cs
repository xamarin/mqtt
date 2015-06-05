using System;

namespace Hermes
{
	public enum ClosedReason
	{
		Disconnected,
		Disposed,
		Error
	}

	public class ClosedEventArgs : EventArgs
	{
		public ClosedEventArgs (ClosedReason reason, string message = null)
		{
			this.Reason = reason;
			this.Message = message;
		}

		public ClosedReason Reason { get; private set; }

		public string Message { get; private set; }
	}
}
