using System;

namespace IntegrationTests.Messages
{
	[Serializable]
	public class TestMessage
	{
		public string Name { get; set; }

		public int Value { get; set; }
	}
}
