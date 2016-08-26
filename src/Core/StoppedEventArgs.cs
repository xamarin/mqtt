namespace System.Net.Mqtt
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
			Reason = reason;
			Message = message;
		}

		public ClosedReason Reason { get; private set; }

		public string Message { get; private set; }
	}
}
