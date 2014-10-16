using System;

namespace Hermes.Messages
{
	public class Connect : Message
	{
		public Connect (string clientId, bool cleanSession) : base(MessageType.Connect)
		{
			if (string.IsNullOrEmpty (clientId)) {
				throw new ArgumentNullException ();
			}

			this.ClientId = clientId;
			this.CleanSession = cleanSession;
			this.KeepAlive = 0;
		}

		public Connect () : base(MessageType.Connect)
		{
			this.CleanSession = true;
			this.KeepAlive = 0;
		}

		public string ClientId { get; private set; }

		public bool CleanSession { get; private set; }

		public int KeepAlive { get; set; }

		public Will Will { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }
	}
}
