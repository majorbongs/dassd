using System;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class PrefixedGuidJsonConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return typeof(PrefixedGuid).IsAssignableFrom(objectType);
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		PrefixedGuid prefixedGuid = (PrefixedGuid)value;
		writer.WriteValue(prefixedGuid.ToString());
	}

	public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		string text = reader.Value?.ToString();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (!Guid.TryParse(text, out var result))
		{
			int num = text.IndexOf('-');
			if (num > 0)
			{
				Guid.TryParse(text.Substring(num + 1), out result);
			}
		}
		return Activator.CreateInstance(objectType, result);
	}
}
