using System;

namespace IntegrationTests.Messages
{
	[Serializable]
	public class TestMessage
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public int Value { get; set; }
	}
}
