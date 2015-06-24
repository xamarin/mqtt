using System;

namespace IntegrationTests.Messages
{
	[Serializable]
	public class ResponseMessage
	{
		public string Name { get; set; }

		public bool Ok { get; set; }
	}
}
