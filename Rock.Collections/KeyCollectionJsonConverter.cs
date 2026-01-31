using System;
using System.Linq;
using Newtonsoft.Json;

namespace Rock.Collections;

public class KeyCollectionJsonConverter<K, V> : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(OrderedDictionary<K, V>.KeyCollection);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		throw new NotSupportedException("NotSupported_KeyCollection");
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is OrderedDictionary<K, V>.KeyCollection source)
		{
			serializer.Serialize(writer, source.ToList());
		}
	}
}
