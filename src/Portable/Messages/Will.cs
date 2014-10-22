using System;

namespace Hermes.Messages
{
	public class Will : IEquatable<Will>
    {
        public Will(string topic, QualityOfService qos, bool retain, string message)
        {
            this.Topic = topic;
            this.QualityOfService = qos;
            this.Retain = retain;
			this.Message = message;
        }

        public string Topic { get; set; }

        public QualityOfService QualityOfService { get; set; }

		public bool Retain { get; set; }

		public string Message { get; set; }

		public bool Equals (Will other)
		{
			if (other == null)
				return false;

			return this.Topic == other.Topic &&
				this.QualityOfService == other.QualityOfService &&
				this.Retain == other.Retain &&
				this.Message == other.Message;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;

			var will = obj as Will;

			if (will == null)
				return false;

			return this.Equals (will);
		}

		public static bool operator == (Will will, Will other)
		{
			if ((object)will == null || (object)other == null)
				return Object.Equals(will, other);

			return will.Equals(other);
		}

		public static bool operator != (Will will, Will other)
		{
			if ((object)will == null || (object)other == null)
				return !Object.Equals(will, other);

			return !will.Equals(other);
		}

		public override int GetHashCode ()
		{
			return this.Topic.GetHashCode () + this.Message.GetHashCode ();
		}
	}
}
