namespace System.Net.Mqtt
{
	public enum DisconnectedReason
	{
		RemoteDisconnected,
		Disposed,
		Error
	}

	public class MqttEndpointDisconnected
	{
		public MqttEndpointDisconnected (DisconnectedReason reason, string message = null)
		{
			Reason = reason;
			Message = message;
		}

		public DisconnectedReason Reason { get; private set; }

		public string Message { get; private set; }
	}
}
