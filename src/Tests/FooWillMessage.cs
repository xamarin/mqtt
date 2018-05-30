using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Tests
{
	[Serializable]
	internal class FooWillMessage
	{
		public string Message { get; set; }

		public static byte[] GetPayload (FooWillMessage willMessage)
		{
			var formatter = new BinaryFormatter ();

			using (var stream = new MemoryStream ()) {
				formatter.Serialize(stream, willMessage);

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
