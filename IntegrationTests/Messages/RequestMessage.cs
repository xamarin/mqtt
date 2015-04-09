using System;

namespace IntegrationTests.Messages
{
	[Serializable]
    public class RequestMessage
    {
		public Guid Id { get; set; }

		public string Name { get; set; }

		public DateTime Date { get; set; }

		public byte[] Content { get; set; }
    }
}
