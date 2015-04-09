using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace IntegrationTests
{
	public class Serializer
	{
		public static byte[] Serialize<T> (T message)
		{
			var result = default (byte[]);

			using(var stream = new MemoryStream()) {
				var formatter = new BinaryFormatter ();

				formatter.Serialize (stream, message);
				result = stream.ToArray ();
			}

			return result;
		}

		public static T Deserialize<T>(byte[] content) 
			where T : class
		{
			var result = default (T);

			using (var stream = new MemoryStream (content)) {
				var formatter = new BinaryFormatter ();

				result = formatter.Deserialize (stream) as T;
			}

			return result;
		}
	}
}
