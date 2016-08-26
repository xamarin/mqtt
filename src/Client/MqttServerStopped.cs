namespace System.Net.Mqtt
{
	public enum StoppedReason
	{
		Disconnected,
		Disposed,
		Error
	}

	public class MqttServerStopped
	{
		public MqttServerStopped (StoppedReason reason, string message = null)
		{
			Reason = reason;
			Message = message;
		}

		public StoppedReason Reason { get; private set; }

		public string Message { get; private set; }
	}
}
