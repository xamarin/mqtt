using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Tests
{
	[Serializable]
	internal class FooWillMessage
	{
		public string Message { get; set; }

		public byte[] GetPayload ()
		{
			var formatter = new BinaryFormatter ();

			using (var stream = new MemoryStream ()) {
				formatter.Serialize(stream, this);

				return stream.ToArray();
			}
		}

		public static FooWillMessage GetMessage (byte[] willPayload)
		{
			var formatter = new BinaryFormatter ();

			using (var stream = new MemoryStream (willPayload)) {
				return formatter.Deserialize (stream) as FooWillMessage;
			}
		}
	}
}
