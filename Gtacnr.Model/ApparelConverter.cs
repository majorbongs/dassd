using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gtacnr.Model;

public class ApparelConverter : JsonConverter<Apparel>
{
	public override void WriteJson(JsonWriter writer, Apparel value, JsonSerializer serializer)
	{
		writer.WriteStartArray();
		foreach (string item in value.Items)
		{
			writer.WriteValue(item);
		}
		writer.WriteEndArray();
	}

	public override Apparel ReadJson(JsonReader reader, Type objectType, Apparel existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.StartArray)
		{
			new Apparel();
			return new Apparel(JToken.Load(reader).ToObject<List<string>>());
		}
		return null;
	}
}
