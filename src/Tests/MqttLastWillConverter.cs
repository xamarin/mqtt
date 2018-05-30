using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Mqtt;
using System.Text;

namespace Tests
{
	internal class MqttLastWillConverter : JsonConverter
	{
		public override bool CanWrite => false;

		public override bool CanConvert (Type objectType) => objectType == typeof (MqttLastWill);

		public override object ReadJson (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var jObject = JObject.Load (reader);

			var topic = jObject["topic"].ToObject<string> ();
			var qos = jObject["qualityOfService"].ToObject<MqttQualityOfService> ();
			var retain = jObject["retain"].ToObject<bool> ();
			var message = jObject["message"].ToObject<string> ();
			var hasMessage = !string.IsNullOrEmpty ((message));
			var payload = hasMessage ? Encoding.UTF8.GetBytes (message) : jObject["message"].ToObject<byte[]> ();

			return new MqttLastWill (topic, qos, retain, payload);
		}

		public override void WriteJson (JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
}
