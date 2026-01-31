using System;
using System.Linq;
using Newtonsoft.Json;

namespace Rock.Collections;

public class ValueCollectionJsonConverter<K, V> : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(OrderedDictionary<K, V>.ValueCollection);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		throw new NotSupportedException("NotSupported_ValueCollectionSet");
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is OrderedDictionary<K, V>.ValueCollection source)
		{
			serializer.Serialize(writer, source.ToList());
		}
	}
}
