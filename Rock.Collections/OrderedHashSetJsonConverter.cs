using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Rock.Collections;

public class OrderedHashSetJsonConverter<T> : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(OrderedHashSet<T>);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		return new OrderedHashSet<T>(JToken.Load(reader).ToObject<List<T>>(serializer));
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is OrderedHashSet<T> source)
		{
			serializer.Serialize(writer, source.ToList());
		}
	}
}
