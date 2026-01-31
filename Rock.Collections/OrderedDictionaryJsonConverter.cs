using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Rock.Collections;

public class OrderedDictionaryJsonConverter<K, V> : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(OrderedDictionary<K, V>);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		JToken jToken = JToken.Load(reader);
		OrderedDictionary<K, V> orderedDictionary = new OrderedDictionary<K, V>();
		foreach (JProperty item in jToken.Children<JProperty>())
		{
			K key = serializer.Deserialize<K>(new JTokenReader(item.Name));
			V value = serializer.Deserialize<V>(new JTokenReader(item.Value));
			orderedDictionary.Add(key, value);
		}
		return orderedDictionary;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		OrderedDictionary<K, V> obj = value as OrderedDictionary<K, V>;
		writer.WriteStartObject();
		foreach (KeyValuePair<K, V> item in obj)
		{
			JToken jToken = JToken.FromObject(item.Key, serializer);
			writer.WritePropertyName(jToken.ToString());
			serializer.Serialize(writer, item.Value);
		}
		writer.WriteEndObject();
	}
}
