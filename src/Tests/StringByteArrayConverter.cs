using System;
using System.Text;
using Newtonsoft.Json;

namespace Tests
{
	public class StringByteArrayConverter : JsonConverter
	{
		public override bool CanConvert (Type objectType)
		{
			return objectType == typeof (byte[]);
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return Encoding.UTF8.GetBytes (reader.Value.ToString ());
		}

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			var bytes = value as byte[];

			if(bytes != null) {
				writer.WriteValue (Encoding.UTF8.GetString (bytes));
			}
		}
	}
}
