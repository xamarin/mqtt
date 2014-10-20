using System;
using Hermes.Messages;
using Newtonsoft.Json;

namespace Hermes
{
	public class QualityOfServiceConverter : JsonConverter
	{
		public override bool CanConvert (Type objectType)
		{
			return objectType == typeof (QualityOfService);
		}

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var result = default(QualityOfService);

			Enum.TryParse(reader.Value.ToString(), out result);

			return result;
		}

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue (((QualityOfService)value));
		}
	}
}
