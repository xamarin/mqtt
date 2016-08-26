namespace System.Net.Mqtt.Packets
{
	public class MqttLastWill : IEquatable<MqttLastWill>
	{
		public MqttLastWill (string topic, MqttQualityOfService qos, bool retain, string message)
		{
			Topic = topic;
			QualityOfService = qos;
			Retain = retain;
			Message = message;
		}

		public string Topic { get; set; }

		public MqttQualityOfService QualityOfService { get; set; }

		public bool Retain { get; set; }

		public string Message { get; set; }

		public bool Equals (MqttLastWill other)
		{
			if (other == null)
				return false;

			return Topic == other.Topic &&
				QualityOfService == other.QualityOfService &&
				Retain == other.Retain &&
				Message == other.Message;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var will = obj as MqttLastWill;

			if (will == null)
				return false;

			return Equals (will);
		}

		public static bool operator == (MqttLastWill will, MqttLastWill other)
		{
			if ((object)will == null || (object)other == null)
				return Object.Equals (will, other);

			return will.Equals (other);
		}

		public static bool operator != (MqttLastWill will, MqttLastWill other)
		{
			if ((object)will == null || (object)other == null)
				return !Object.Equals (will, other);

			return !will.Equals (other);
		}

		public override int GetHashCode ()
		{
			return Topic.GetHashCode () + Message.GetHashCode ();
		}
	}
}
